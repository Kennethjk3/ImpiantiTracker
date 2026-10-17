import { getJson } from '#lib/server/api.ts';
import type { InstallationRow, MaintenancePlan } from '#lib/types.ts';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
	const city = url.searchParams.get('city')?.trim() || null;
	const query = city ? `?city=${encodeURIComponent(city)}` : '';

	// The API filters by city; the full plan gives every installation's status and the list of cities.
	const [rows, plan] = await Promise.all([
		getJson<InstallationRow[]>(fetch, `/api/installations${query}`),
		getJson<MaintenancePlan>(fetch, '/api/maintenance?all=true')
	]);

	const due = Object.fromEntries(plan.items.map((item) => [item.installationId, item]));
	const cities = [...new Set(plan.items.map((item) => item.city))].sort((a, b) => a.localeCompare(b, 'it'));

	return {
		city,
		cities,
		installations: rows.map((row) => ({ ...row, due: due[row.id] ?? null }))
	};
};
