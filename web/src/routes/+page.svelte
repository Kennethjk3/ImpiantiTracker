<script lang="ts">
	import StatusLabel from '#lib/components/StatusLabel.svelte';
	import ThermalStrip from '#lib/components/ThermalStrip.svelte';
	import { WINDOWS, formatDate, formatLongDate, relativeDue, telHref, typeLabels } from '#lib/format.ts';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	const attention = $derived(data.plan.items.filter((i) => i.status !== 'Ok'));
	const overdue = $derived(attention.filter((i) => i.status === 'Overdue').length);
	const dueSoon = $derived(attention.length - overdue);

	const count = (n: number) => (n === 1 ? '1 installation' : `${n} installations`);
	const summary = $derived.by(() => {
		const window = `in the next ${data.days} days`;
		if (overdue === 0 && dueSoon === 0) return `Nothing is overdue or due ${window}.`;
		if (overdue === 0) return `Nothing is overdue. ${count(dueSoon)} ${dueSoon === 1 ? 'is' : 'are'} due ${window}.`;
		if (dueSoon === 0) return `${count(overdue)} ${overdue === 1 ? 'is' : 'are'} overdue. Nothing else is due ${window}.`;
		return `${count(overdue)} ${overdue === 1 ? 'is' : 'are'} overdue and ${dueSoon} more ${dueSoon === 1 ? 'is' : 'are'} due ${window}.`;
	});
</script>

<svelte:head>
	<title>Maintenance plan – Impianti</title>
</svelte:head>

<div class="page-head">
	<div>
		<h1>Maintenance plan</h1>
		<p class="muted">{formatLongDate(data.plan.asOf)}</p>
	</div>
	<nav class="window" aria-label="Days to look ahead">
		<span class="muted" aria-hidden="true">Days ahead</span>
		<div class="segmented">
			{#each WINDOWS as days (days)}
				<a
					href="?days={days}"
					aria-current={days === data.days ? 'page' : undefined}
					aria-label="{days} days"
					data-sveltekit-reset="false">{days}</a
				>
			{/each}
		</div>
	</nav>
</div>

<p class="summary">{summary}</p>

<ThermalStrip items={data.plan.items} windowDays={data.days} />

<section class="section" aria-labelledby="attention">
	<div class="section-head">
		<h2 id="attention">Needs attention</h2>
		<a class="button" href="/export.csv?days={data.days}" download>Download for Excel</a>
	</div>

	{#if attention.length === 0}
		<p class="panel empty">
			Every installation is serviced and none is due in the next {data.days} days. Try a longer window to plan ahead.
		</p>
	{:else}
		<div class="panel table-wrap">
			<table class="stack">
				<thead>
					<tr>
						<th scope="col">Customer</th>
						<th scope="col">Status</th>
						<th scope="col">Service due</th>
						<th scope="col">Phone</th>
						<th scope="col">Installation</th>
						<th scope="col">Last service</th>
					</tr>
				</thead>
				<tbody>
					{#each attention as item (item.installationId)}
						<tr>
							<td class="lead">
								<a class="customer" href="/installations/{item.installationId}">{item.customer}</a>
								<span class="muted city">{item.city}</span>
							</td>
							<td data-label="Status"><StatusLabel status={item.status} /></td>
							<td data-label="Service due">
								<span>
									<span class="nowrap">{formatDate(item.nextDue)}</span>
									<span class="muted small rel {item.status}">{relativeDue(item.daysUntilDue)}</span>
								</span>
							</td>
							<td data-label="Phone" class="nowrap">
								{#if item.phone}<a href={telHref(item.phone)}>{item.phone}</a>{:else}<span class="muted">None</span>{/if}
							</td>
							<td data-label="Installation">{typeLabels[item.type]}, {item.brand}</td>
							<td data-label="Last service" class="nowrap">{formatDate(item.lastService)}</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</section>

<style>
	.window {
		display: flex;
		align-items: center;
		gap: var(--space-3);
	}

	.summary {
		margin-bottom: var(--space-5);
		font-size: var(--step-1);
		line-height: 1.35;
		max-width: 62ch;
	}

	.customer {
		font-weight: 650;
		color: var(--ink);
	}

	.city,
	.rel {
		display: block;
	}

	.rel.Overdue {
		color: var(--hot);
	}

	.empty {
		padding: var(--space-5);
	}

	@media (max-width: 44rem) {
		table.stack tr {
			display: flex;
			flex-direction: column;
		}

		.city {
			font-size: var(--step-0);
		}

		.rel {
			display: inline;
			margin-left: var(--space-2);
		}
	}
</style>
