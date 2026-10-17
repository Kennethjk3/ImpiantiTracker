import { error } from '@sveltejs/kit';
import { API_URL } from '$app/env/private';
import type { Problem } from '../types.ts';

// Server-only (it lives in a "server" folder): the browser talks to SvelteKit, SvelteKit talks to the API.
// So the API needs no CORS setup and its address never reaches the browser.

type Fetch = typeof fetch;

async function send(fetch: Fetch, path: string, init?: RequestInit): Promise<Response> {
	try {
		return await fetch(`${API_URL}${path}`, init);
	} catch {
		error(503, `The API isn't answering at ${API_URL}. Start it with: dotnet run --project src/ImpiantiTracker.Api`);
	}
}

/** GET a JSON resource. A 404 becomes this app's 404 page; any other failure, a 502. */
export async function getJson<T>(fetch: Fetch, path: string): Promise<T> {
	const response = await send(fetch, path, { headers: { accept: 'application/json' } });
	if (response.status === 404) {
		const problem = await readProblem(response);
		error(404, problem?.detail ?? 'Not found.');
	}
	if (!response.ok) error(502, `The API answered ${response.status} to ${path}.`);
	return (await response.json()) as T;
}

/** POST JSON and hand back the raw response, so the caller can treat 201, 400 and 404 differently. */
export function postJson(fetch: Fetch, path: string, body: unknown): Promise<Response> {
	return send(fetch, path, {
		method: 'POST',
		headers: { 'content-type': 'application/json', accept: 'application/json' },
		body: JSON.stringify(body)
	});
}

/** GET anything (the CSV export) and pass the response through. */
export async function getRaw(fetch: Fetch, path: string): Promise<Response> {
	const response = await send(fetch, path);
	if (!response.ok) error(502, `The API answered ${response.status} to ${path}.`);
	return response;
}

export async function readProblem(response: Response): Promise<Problem | null> {
	try {
		return (await response.json()) as Problem;
	} catch {
		return null;
	}
}

/** Every message in a validation problem, flattened into one list. */
export function problemMessages(problem: Problem | null): string[] {
	if (!problem) return ['The API rejected the request.'];
	const messages = Object.values(problem.errors ?? {}).flat();
	if (messages.length > 0) return messages;
	return [problem.detail ?? problem.title ?? 'The API rejected the request.'];
}
