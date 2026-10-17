import { parseWindow } from '#lib/format.ts';
import { getJson } from '#lib/server/api.ts';
import type { MaintenancePlan } from '#lib/types.ts';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
	const days = parseWindow(url.searchParams.get('days'));
	// all=true: the strip plots every installation; the table shows only those needing attention.
	const plan = await getJson<MaintenancePlan>(fetch, `/api/maintenance?days=${days}&all=true`);
	return { plan, days };
};
