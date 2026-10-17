<script lang="ts">
	import '@fontsource-variable/archivo/wdth.css';
	import '../app.css';
	import favicon from '#lib/assets/favicon.svg';
	import { page } from '$app/state';
	import type { LayoutProps } from './$types';

	let { children }: LayoutProps = $props();

	const nav = [
		{ href: '/', label: 'Plan' },
		{ href: '/installations', label: 'Installations' },
		{ href: '/reports', label: 'Report' }
	];

	const isCurrent = (href: string) =>
		href === '/' ? page.url.pathname === '/' : page.url.pathname.startsWith(href);
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<a class="skip" href="#main">Skip to content</a>

<header>
	<div class="bar">
		<a class="brand" href="/">
			<img src={favicon} alt="" width="28" height="28" />
			<span>Impianti</span>
		</a>
		<nav aria-label="Main">
			{#each nav as item (item.href)}
				<a href={item.href} aria-current={isCurrent(item.href) ? 'page' : undefined}>{item.label}</a>
			{/each}
		</nav>
	</div>
</header>

<main id="main" class="page">
	{@render children()}
</main>

<style>
	.skip {
		position: absolute;
		left: var(--space-4);
		top: -3rem;
		z-index: 10;
		padding: var(--space-2) var(--space-3);
		background: var(--ink);
		color: var(--paper);
	}

	.skip:focus {
		top: var(--space-2);
	}

	header {
		background: var(--surface);
		border-bottom: 1px solid var(--rule);
	}

	.bar {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		justify-content: space-between;
		gap: var(--space-2) var(--space-5);
		width: min(100% - 2rem, var(--page));
		margin-inline: auto;
		min-height: 4rem;
	}

	.brand {
		display: inline-flex;
		align-items: center;
		gap: var(--space-2);
		color: var(--ink);
		text-decoration: none;
		font-size: var(--step-1);
		font-weight: 800;
		font-stretch: var(--expanded);
		letter-spacing: -0.01em;
	}

	nav {
		display: flex;
		gap: var(--space-1);
		margin-inline-end: calc(var(--space-3) * -1);
	}

	nav a {
		padding: var(--space-3);
		color: var(--ink-2);
		text-decoration: none;
		font-weight: 600;
		font-stretch: var(--condensed);
		font-size: var(--step-1);
		border-bottom: 3px solid transparent;
	}

	nav a:hover {
		color: var(--ink);
	}

	nav a[aria-current='page'] {
		color: var(--ink);
		border-bottom-color: var(--ink);
	}
</style>
