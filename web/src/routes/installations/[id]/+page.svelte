<script lang="ts">
	import { enhance } from '$app/forms';
	import StatusLabel from '#lib/components/StatusLabel.svelte';
	import {
		formatDate,
		formatKw,
		formatMoney,
		kindLabels,
		relativeDue,
		telHref,
		typeLabels
	} from '#lib/format.ts';
	import type { VisitKind } from '#lib/types.ts';
	import type { PageProps } from './$types';

	let { data, form }: PageProps = $props();

	const i = $derived(data.installation);
	const due = $derived(i.due);
	const title = $derived(i.model ? `${i.brand} ${i.model}` : i.brand);

	let saving = $state(false);

	const kinds: VisitKind[] = ['Maintenance', 'EfficiencyCheck', 'Repair'];
	const resetsClock = (kind: VisitKind) => kind !== 'Repair';
</script>

<svelte:head>
	<title>{title}, {i.customer.name} – Impianti</title>
</svelte:head>

<p class="back"><a href="/installations">All installations</a></p>

<div class="page-head">
	<div>
		<h1>{title}</h1>
		<p class="muted">{typeLabels[i.type]} at {i.customer.name}, {i.customer.city}</p>
	</div>
</div>

<div class="layout">
	<section class="tag {due.status}" aria-labelledby="next-service">
		<h2 id="next-service" class="tag-title">Next service</h2>
		<p class="tag-date">{formatDate(due.nextDue)}</p>
		<p class="tag-status">
			<StatusLabel status={due.status} />
			<span>{relativeDue(due.daysUntilDue)}</span>
		</p>
		<p class="muted small">
			{#if due.lastService}
				Last serviced {formatDate(due.lastService)}, every {i.maintenanceIntervalMonths} months.
			{:else}
				Not serviced yet. The first service is due {i.maintenanceIntervalMonths} months after installation.
			{/if}
		</p>
	</section>

	<section class="record panel" aria-labelledby="record">
		<h2 id="record">Record a visit</h2>

		{#if form?.saved}
			<p class="notice success" role="status">
				Visit saved. Next service due {formatDate(form.nextDue)}.
			</p>
		{:else if form?.errors}
			<div class="notice error" role="alert">
				The visit wasn't saved:
				<ul>
					{#each form.errors as message (message)}<li>{message}</li>{/each}
				</ul>
			</div>
		{/if}

		<form
			method="POST"
			use:enhance={() => {
				saving = true;
				return async ({ update }) => {
					await update();
					saving = false;
				};
			}}
		>
			<div class="row">
				<div class="field">
					<label for="date">Date</label>
					<!-- defaultValue is what the form goes back to when it's reset after a save: today. -->
					<input
						id="date"
						name="date"
						type="date"
						max={data.today}
						value={form?.values?.date || data.today}
						defaultValue={data.today}
						required
					/>
				</div>
				<div class="field">
					<label for="kind">Type of visit</label>
					<select id="kind" name="kind">
						{#each kinds as kind (kind)}
							<option value={kind} selected={(form?.values?.kind ?? 'Maintenance') === kind}>{kindLabels[kind]}</option>
						{/each}
					</select>
				</div>
			</div>
			<p class="hint small muted">Maintenance and efficiency checks restart the service interval. Repairs don't.</p>

			<div class="row">
				<div class="field">
					<label for="technician">Technician</label>
					<input
						id="technician"
						name="technician"
						list="technicians"
						autocomplete="off"
						value={form?.values?.technician ?? ''}
						required
					/>
					<datalist id="technicians">
						{#each data.technicians as name (name)}<option value={name}></option>{/each}
					</datalist>
				</div>
				<div class="field">
					<label for="cost">Cost (€)</label>
					<input id="cost" name="cost" inputmode="decimal" placeholder="0,00" value={form?.values?.cost ?? ''} />
				</div>
			</div>

			<div class="field">
				<label for="notes">Notes</label>
				<textarea id="notes" name="notes" maxlength="500" placeholder="What was done, parts replaced">{form?.values?.notes ?? ''}</textarea>
			</div>

			<button class="button primary" type="submit" disabled={saving}>{saving ? 'Saving visit…' : 'Save visit'}</button>
		</form>
	</section>

	<section class="facts" aria-labelledby="details">
		<h2 id="details" class="visually-hidden">Details</h2>
		<div class="panel columns">
			<dl>
				<dt>Power</dt>
				<dd>{formatKw(i.powerKw)} kW</dd>
				<dt>Installed</dt>
				<dd>{formatDate(i.installedOn)}</dd>
				<dt>Serial number</dt>
				<dd>{i.serialNumber ?? 'Not recorded'}</dd>
				{#if i.registryCode}
					<dt>Registry code</dt>
					<dd>{i.registryCode}</dd>
				{/if}
			</dl>
			<div class="customer">
				<h3>{i.customer.name}</h3>
				<p>{i.customer.address}, {i.customer.city}</p>
				{#if i.customer.phone}<p><a href={telHref(i.customer.phone)}>{i.customer.phone}</a></p>{/if}
				{#if i.customer.email}<p><a href="mailto:{i.customer.email}">{i.customer.email}</a></p>{/if}
			</div>
		</div>
	</section>

	<section class="history" aria-labelledby="history">
		<h2 id="history">Visit history</h2>
		{#if i.visits.length === 0}
			<p class="muted">No visits recorded yet. Record the first one with the form.</p>
		{:else}
			<ol class="timeline">
				{#each i.visits as visit (visit.id)}
					<li class:service={resetsClock(visit.kind)}>
						<div class="when">{formatDate(visit.date)}</div>
						<div class="what">
							<p class="kind">
								{kindLabels[visit.kind]}
								<span class="cost">{formatMoney(visit.cost)}</span>
							</p>
							<p class="muted small">{visit.technician}</p>
							{#if visit.notes}<p class="notes">{visit.notes}</p>{/if}
						</div>
					</li>
				{/each}
			</ol>
			<p class="small muted legend">
				<span class="dot service" aria-hidden="true"></span> restarts the service interval
				<span class="dot" aria-hidden="true"></span> repair
			</p>
		{/if}
	</section>
</div>

<style>
	.back {
		margin-bottom: var(--space-3);
	}

	.layout {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(20rem, 26rem);
		grid-template-areas:
			'tag record'
			'facts record'
			'history record';
		align-items: start;
		gap: var(--space-5) var(--space-6);
	}

	.tag {
		grid-area: tag;
	}

	.record {
		grid-area: record;
		position: sticky;
		top: var(--space-4);
	}

	.facts {
		grid-area: facts;
	}

	.history {
		grid-area: history;
	}

	@media (max-width: 56rem) {
		.layout {
			grid-template-columns: minmax(0, 1fr);
			grid-template-areas: 'tag' 'record' 'facts' 'history';
		}

		.record {
			position: static;
		}
	}

	/* The service tag: like the card that hangs on a boiler, with its hole. */
	.tag {
		position: relative;
		display: grid;
		gap: var(--space-2);
		padding: var(--space-5) var(--space-5) var(--space-5) calc(var(--space-5) + 1.25rem);
		background: var(--surface);
		border: 1px solid var(--rule);
		border-left: 6px solid var(--cool-mark);
		border-radius: var(--radius) 1.25rem 1.25rem var(--radius);
	}

	.tag::before {
		content: '';
		position: absolute;
		left: 0.9rem;
		top: 50%;
		width: 0.85rem;
		height: 0.85rem;
		margin-top: -0.425rem;
		border-radius: 50%;
		background: var(--paper);
		box-shadow: inset 0 0 0 1px var(--rule-strong);
	}

	.tag.Overdue {
		border-left-color: var(--hot-mark);
	}

	.tag.DueSoon {
		border-left-color: var(--warm-mark);
	}

	.tag-title {
		font-size: var(--step-0);
		color: var(--ink-2);
	}

	.tag-date {
		font-size: var(--step-4);
		font-weight: 750;
		font-stretch: var(--expanded);
		line-height: 1;
		letter-spacing: -0.02em;
	}

	.tag-status {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-2) var(--space-3);
		font-size: var(--step-1);
	}

	.record {
		display: grid;
		gap: var(--space-4);
		padding: var(--space-5);
	}

	.record form {
		display: grid;
		gap: var(--space-4);
	}

	.row {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: var(--space-3);
	}

	.hint {
		margin-top: calc(var(--space-2) * -1);
	}

	.record .button {
		justify-self: start;
	}

	.columns {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr));
		gap: var(--space-5);
		padding: var(--space-5);
	}

	dl {
		display: grid;
		grid-template-columns: auto 1fr;
		gap: var(--space-2) var(--space-4);
		margin: 0;
	}

	dt {
		color: var(--ink-2);
		font-stretch: var(--condensed);
		font-weight: 600;
	}

	dd {
		margin: 0;
	}

	.customer {
		display: grid;
		gap: var(--space-1);
		align-content: start;
	}

	.customer h3 {
		margin-bottom: var(--space-1);
	}

	.history h2 {
		margin-bottom: var(--space-4);
	}

	/* A visit history is a sequence in time, so it's drawn as one. */
	.timeline {
		list-style: none;
		margin: 0;
		padding: 0;
	}

	.timeline li {
		position: relative;
		display: grid;
		grid-template-columns: 6.5rem minmax(0, 1fr);
		gap: var(--space-4);
		padding: 0 0 var(--space-5) 1.75rem;
	}

	.timeline li::before {
		content: '';
		position: absolute;
		left: 0.4rem;
		top: 0.75rem;
		bottom: 0;
		width: 2px;
		background: var(--rule);
	}

	.timeline li:last-child::before {
		display: none;
	}

	.timeline li::after,
	.dot {
		content: '';
		width: 0.95rem;
		height: 0.95rem;
		border-radius: 50%;
		border: 2px solid var(--cool-mark);
		background: var(--paper);
	}

	.timeline li::after {
		position: absolute;
		left: 0;
		top: 0.3rem;
	}

	.timeline li.service::after,
	.dot.service {
		background: var(--cool-mark);
	}

	.when {
		font-weight: 650;
		font-stretch: var(--condensed);
	}

	.kind {
		display: flex;
		flex-wrap: wrap;
		justify-content: space-between;
		gap: var(--space-2);
		font-weight: 600;
		max-width: none;
	}

	.cost {
		font-weight: 500;
		color: var(--ink-2);
	}

	.notes {
		margin-top: var(--space-1);
	}

	.legend {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-2);
	}

	.dot {
		display: inline-block;
		vertical-align: middle;
		margin-left: var(--space-2);
	}

	.dot:first-child {
		margin-left: 0;
	}

	@media (max-width: 30rem) {
		.row {
			grid-template-columns: minmax(0, 1fr);
		}

		.timeline li {
			grid-template-columns: minmax(0, 1fr);
			gap: var(--space-1);
		}

		.tag-date {
			font-size: var(--step-3);
		}
	}
</style>
