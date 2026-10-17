<script lang="ts">
	import StatusLabel from '#lib/components/StatusLabel.svelte';
	import { formatDate, formatKw, typeLabels } from '#lib/format.ts';
	import type { PageProps } from './$types';

	let { data }: PageProps = $props();

	// Typing in the search box narrows the list instantly, in the browser; the city filter goes to the API.
	let search = $state('');
	const shown = $derived.by(() => {
		const words = search.trim().toLowerCase();
		if (!words) return data.installations;
		return data.installations.filter((i) =>
			`${i.customer} ${i.brand} ${i.model ?? ''} ${typeLabels[i.type]}`.toLowerCase().includes(words)
		);
	});

	const customers = $derived(new Set(data.installations.map((i) => i.customer)).size);
	const count = (n: number, word: string) => `${n} ${word}${n === 1 ? '' : 's'}`;

	function submitOnChange(event: Event) {
		(event.currentTarget as HTMLSelectElement).form?.requestSubmit();
	}
</script>

<svelte:head>
	<title>Installations – Impianti</title>
</svelte:head>

<div class="page-head">
	<div>
		<h1>Installations</h1>
		<p class="muted">
			{count(data.installations.length, 'installation')} at {count(customers, 'customer')}{data.city ? ` in ${data.city}` : ''}.
		</p>
	</div>
</div>

<div class="filters">
	<form method="GET" class="city-filter" data-sveltekit-reset="false">
		<div class="field">
			<label for="city">City</label>
			<select id="city" name="city" onchange={submitOnChange}>
				<option value="" selected={!data.city}>All cities</option>
				{#each data.cities as city (city)}
					<option value={city} selected={city === data.city}>{city}</option>
				{/each}
			</select>
		</div>
		<noscript><button class="button" type="submit">Show</button></noscript>
	</form>

	<div class="field search">
		<label for="search">Find</label>
		<input id="search" type="search" placeholder="Customer, brand or type" bind:value={search} autocomplete="off" />
	</div>
</div>

{#if shown.length === 0}
	<p class="panel empty">
		{#if search}No installation matches “{search}”.{:else}No installations in {data.city}.{/if}
	</p>
{:else}
	<div class="panel table-wrap">
		<table class="stack">
			<thead>
				<tr>
					<th scope="col">Customer</th>
					<th scope="col">Installation</th>
					<th scope="col" class="num">kW</th>
					<th scope="col">Installed</th>
					<th scope="col">Last visit</th>
					<th scope="col">Next service</th>
				</tr>
			</thead>
			<tbody>
				{#each shown as row (row.id)}
					<tr>
						<td class="lead">
							<a class="customer" href="/installations/{row.id}">{row.customer}</a>
							<span class="muted place">{row.city}</span>
						</td>
						<td data-label="Installation">
							<span>
								{typeLabels[row.type]}
								<span class="muted model">{row.brand}{row.model ? ` ${row.model}` : ''}</span>
							</span>
						</td>
						<td data-label="kW" class="num">{formatKw(row.powerKw)}</td>
						<td data-label="Installed" class="nowrap">{formatDate(row.installedOn)}</td>
						<td data-label="Last visit" class="nowrap">{formatDate(row.lastVisit)}</td>
						<td data-label="Next service">
							{#if row.due}
								<span>
									<StatusLabel status={row.due.status} />
									<span class="muted small date">{formatDate(row.due.nextDue)}</span>
								</span>
							{/if}
						</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
{/if}

<style>
	.filters {
		display: flex;
		flex-wrap: wrap;
		align-items: end;
		gap: var(--space-4);
		margin-bottom: var(--space-4);
	}

	.city-filter {
		display: flex;
		align-items: end;
		gap: var(--space-2);
	}

	.city-filter select {
		min-width: 13rem;
	}

	.search {
		flex: 1 1 16rem;
		max-width: 24rem;
	}

	.customer {
		font-weight: 650;
		color: var(--ink);
	}

	.place,
	.model,
	.date {
		display: block;
	}

	.empty {
		padding: var(--space-5);
	}

	@media (max-width: 44rem) {
		table.stack td.num {
			text-align: left;
		}

		.search {
			max-width: none;
		}

		.city-filter select {
			min-width: 0;
		}
	}
</style>
