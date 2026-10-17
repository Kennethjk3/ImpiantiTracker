import { parseWindow } from '#lib/format.ts';
import { getRaw } from '#lib/server/api.ts';
import type { RequestHandler } from './$types';

/** Passes the API's CSV through, so the download works without the browser knowing where the API is. */
export const GET: RequestHandler = async ({ fetch, url }) => {
	const days = parseWindow(url.searchParams.get('days'));
	const upstream = await getRaw(fetch, `/api/maintenance/export?days=${days}`);

	return new Response(upstream.body, {
		headers: {
			'content-type': upstream.headers.get('content-type') ?? 'text/csv; charset=utf-8',
			'content-disposition': upstream.headers.get('content-disposition') ?? 'attachment; filename=maintenance-plan.csv',
			'cache-control': 'no-store'
		}
	});
};
