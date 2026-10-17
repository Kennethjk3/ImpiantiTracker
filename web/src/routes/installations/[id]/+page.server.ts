import { error, fail } from '@sveltejs/kit';
import { addDays, parseEuro } from '#lib/format.ts';
import { getJson, postJson, problemMessages, readProblem } from '#lib/server/api.ts';
import type { InstallationDetail, VisitCreated, VisitKind } from '#lib/types.ts';
import type { Actions, PageServerLoad } from './$types';

const KINDS: VisitKind[] = ['Maintenance', 'EfficiencyCheck', 'Repair'];

function installationId(raw: string): number {
	const id = Number(raw);
	if (!Number.isInteger(id) || id <= 0) error(404, `There is no installation ${raw}.`);
	return id;
}

export const load: PageServerLoad = async ({ fetch, params }) => {
	const id = installationId(params.id);
	const installation = await getJson<InstallationDetail>(fetch, `/api/installations/${id}`);
	// The API's "today": the due date minus the days until it. Used as the form's default and latest date.
	const today = addDays(installation.due.nextDue, -installation.due.daysUntilDue);
	// Technicians who have worked here before, offered as suggestions in the form.
	const technicians = [...new Set(installation.visits.map((v) => v.technician))];
	return { installation, today, technicians };
};

export const actions: Actions = {
	// One form, one action: record a visit. Works without JavaScript; use:enhance makes it smoother.
	default: async ({ fetch, params, request }) => {
		const id = installationId(params.id);
		const form = await request.formData();
		const values = {
			date: String(form.get('date') ?? ''),
			kind: String(form.get('kind') ?? ''),
			technician: String(form.get('technician') ?? '').trim(),
			cost: String(form.get('cost') ?? ''),
			notes: String(form.get('notes') ?? '').trim()
		};

		// Catch what's obviously missing here, with field-level messages; the API checks the rest.
		const errors: string[] = [];
		if (!KINDS.includes(values.kind as VisitKind)) errors.push('Choose the type of visit.');
		if (!values.technician) errors.push("Enter the technician's name.");
		const cost = parseEuro(values.cost);
		if (cost === null) errors.push('Enter the cost as a number, like 80 or 80,50.');
		if (errors.length > 0) return fail(400, { values, errors });

		const response = await postJson(fetch, `/api/installations/${id}/visits`, {
			date: values.date || null,
			kind: values.kind,
			technician: values.technician,
			cost,
			notes: values.notes || null
		});

		if (response.status === 404) error(404, `There is no installation ${id}.`);
		if (response.status === 400) {
			return fail(400, { values, errors: problemMessages(await readProblem(response)) });
		}
		if (!response.ok) error(502, `The API answered ${response.status} while saving the visit.`);

		const created = (await response.json()) as VisitCreated;
		return { saved: true, nextDue: created.due.nextDue };
	}
};
