import { defineEnvVars } from '@sveltejs/kit/env';

export const variables = defineEnvVars({
	API_URL: {
		description:
			'Where the ImpiantiTracker API runs. Only the SvelteKit server calls it; the browser never does.',
		schema: (value) => (value?.trim() || 'http://localhost:5080').replace(/\/+$/, '')
	}
});
