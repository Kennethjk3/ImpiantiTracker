import { getJson } from '#lib/server/api.ts';
import type { YearReport } from '#lib/types.ts';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
	const thisYear = new Date().getFullYear();
	const asked = Number(url.searchParams.get('year'));
	const year = Number.isInteger(asked) && asked >= 2000 && asked <= thisYear ? asked : thisYear;

	const report = await getJson<YearReport>(fetch, `/api/reports/${year}`);
	return { report, years: [thisYear - 2, thisYear - 1, thisYear] };
};
