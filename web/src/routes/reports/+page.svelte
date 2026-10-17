<script lang="ts">
	import { formatMoney, kindLabels, monthName } from '#lib/format.ts';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	const r = $derived(data.report);

	// All twelve months, including the empty ones, so the year reads left to right.
	const months = $derived(
		Array.from({ length: 12 }, (_, i) => r.byMonth.find((m) => m.month === i + 1) ?? { month: i + 1, visits: 0, revenue: 0 })
	);
	const peak = $derived(Math.max(1, ...months.map((m) => m.revenue)));
	const visits = (n: number) => (n === 1 ? '1 visit' : `${n} visits`);
</script>

<svelte:head>
	<title>Report {r.year} – Impianti</title>
</svelte:head>

<div class="page-head">
	<div>
		<h1>Report {r.year}</h1>
		<p class="summary">
			{#if r.totalVisits === 0}
				No visits recorded in {r.year}.
			{:else}
				{visits(r.totalVisits)} and {formatMoney(r.totalRevenue)} charged in {r.year}.
			{/if}
		</p>
	</div>
	<nav class="segmented" aria-label="Year">
		{#each data.years as year (year)}
			<a href="?year={year}" aria-current={year === r.year ? 'page' : undefined} data-sveltekit-reset="false">{year}</a>
		{/each}
	</nav>
</div>

{#if r.totalVisits > 0}
	<section aria-labelledby="by-month">
		<h2 id="by-month" class="sub">Revenue by month</h2>
		<ol class="chart panel">
			{#each months as m (m.month)}
				<li aria-label="{monthName(m.month)}: {visits(m.visits)}, {formatMoney(m.revenue)}">
					<span class="track" aria-hidden="true">
						{#if m.revenue > 0}<span class="value">{formatMoney(m.revenue)}</span>{/if}
						<span class="bar" class:empty={m.revenue === 0} style:--h={m.revenue / peak}></span>
					</span>
					<span class="month" aria-hidden="true">{monthName(m.month, true)}</span>
				</li>
			{/each}
		</ol>
	</section>

	<div class="two">
		<section aria-labelledby="by-tech">
			<h2 id="by-tech" class="sub">By technician</h2>
			<div class="panel table-wrap">
				<table>
					<thead>
						<tr>
							<th scope="col">Technician</th>
							<th scope="col" class="num">Visits</th>
							<th scope="col" class="num">Revenue</th>
						</tr>
					</thead>
					<tbody>
						{#each r.byTechnician as t (t.technician)}
							<tr>
								<td>{t.technician}</td>
								<td class="num">{t.visits}</td>
								<td class="num nowrap">{formatMoney(t.revenue)}</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</section>

		<section aria-labelledby="by-kind">
			<h2 id="by-kind" class="sub">By type of visit</h2>
			<div class="panel table-wrap">
				<table>
					<thead>
						<tr>
							<th scope="col">Type</th>
							<th scope="col" class="num">Visits</th>
							<th scope="col" class="num">Share</th>
						</tr>
					</thead>
					<tbody>
						{#each r.byKind as k (k.kind)}
							<tr>
								<td>{kindLabels[k.kind]}</td>
								<td class="num">{k.visits}</td>
								<td class="num">{Math.round((k.visits / r.totalVisits) * 100)}%</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		</section>
	</div>
{/if}

<style>
	.summary {
		font-size: var(--step-1);
	}

	.sub {
		font-size: var(--step-1);
		margin-bottom: var(--space-3);
	}

	.chart {
		list-style: none;
		margin: 0 0 var(--space-7);
		padding: var(--space-5) var(--space-4) var(--space-3);
		display: grid;
		grid-template-columns: repeat(12, minmax(0, 1fr));
		gap: var(--space-2);
		align-items: end;
		height: 17rem;
	}

	.chart li {
		display: flex;
		flex-direction: column;
		height: 100%;
		min-width: 0;
	}

	/* Each amount sits on top of its own bar; the tallest bar leaves room for its label. */
	.track {
		flex: 1;
		display: flex;
		flex-direction: column;
		justify-content: flex-end;
		align-items: center;
		min-height: 0;
	}

	.bar {
		width: 100%;
		max-width: 3.5rem;
		height: calc(var(--h) * (100% - 1.6rem));
		min-height: 2px;
		margin-top: var(--space-1);
		background: var(--cool-mark);
		border-radius: 2px 2px 0 0;
	}

	.bar.empty {
		background: var(--rule);
	}

	.value {
		font-size: var(--step--1);
		font-stretch: var(--condensed);
		color: var(--ink-2);
		white-space: nowrap;
	}

	.month {
		font-size: var(--step--1);
		font-stretch: var(--condensed);
		font-weight: 600;
		color: var(--ink-2);
		padding-top: var(--space-1);
		border-top: 1px solid var(--rule-strong);
		width: 100%;
		text-align: center;
	}

	.two {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(20rem, 1fr));
		gap: var(--space-6);
	}

	@media (max-width: 44rem) {
		/* Twelve columns are too narrow for amounts written across: on a phone they run up the bar. */
		.value {
			writing-mode: vertical-rl;
			transform: rotate(180deg);
		}

		.bar {
			height: calc(var(--h) * (100% - 4.5rem));
		}

		.chart {
			height: 19rem;
			gap: 0.2rem;
			padding-inline: var(--space-2);
		}
	}
</style>
