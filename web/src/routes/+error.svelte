<script lang="ts">
	import { page } from '$app/state';

	const heading = $derived(
		page.status === 404
			? 'Not found'
			: page.status === 503
				? "The API isn't running"
				: 'Something went wrong'
	);
</script>

<svelte:head>
	<title>{heading} – Impianti</title>
</svelte:head>

<div class="error">
	<p class="code muted">Error {page.status}</p>
	<h1>{heading}</h1>
	{#if page.error?.message}
		<p class="message">{page.error.message}</p>
	{/if}
	<p><a href="/">Go to the maintenance plan</a></p>
</div>

<style>
	.error {
		display: grid;
		gap: var(--space-4);
		max-width: 44rem;
	}

	.code {
		font-stretch: var(--condensed);
		font-weight: 600;
	}

	.message {
		padding: var(--space-3) var(--space-4);
		background: var(--surface);
		border: 1px solid var(--rule);
		border-left: 4px solid var(--hot-mark);
		border-radius: var(--radius);
		overflow-wrap: anywhere;
	}
</style>
