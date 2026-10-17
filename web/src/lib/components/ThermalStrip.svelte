<script lang="ts">
	import StatusLabel from './StatusLabel.svelte';
	import { relativeDue } from '#lib/format.ts';
	import type { DueItem } from '#lib/types.ts';

	let { items, windowDays }: { items: DueItem[]; windowDays: number } = $props();

	// The scale runs from the most overdue installation (at least 30 days back, rounded to a month)
	// to a month past the look-ahead window (at least 90 days ahead).
	const lo = $derived(
		Math.max(-360, Math.min(-30, Math.floor(Math.min(0, ...items.map((i) => i.daysUntilDue)) / 30) * 30))
	);
	const hi = $derived(Math.max(90, Math.ceil((windowDays + 30) / 30) * 30));
	const x = (days: number) => ((Math.min(Math.max(days, lo), hi) - lo) / (hi - lo)) * 100;

	// Markers that would overlap move down a row (greedy packing, 3.2% of the width per marker).
	const GAP = 3.2;
	const placed = $derived.by(() => {
		const rowEnds: number[] = [];
		return items
			.filter((i) => i.daysUntilDue <= hi)
			.toSorted((a, b) => a.daysUntilDue - b.daysUntilDue)
			.map((item) => {
				const left = x(item.daysUntilDue);
				let row = rowEnds.findIndex((end) => left - end >= GAP);
				if (row === -1) row = rowEnds.push(left) - 1;
				else rowEnds[row] = left;
				return { item, left, row };
			});
	});
	const rows = $derived(Math.max(1, ...placed.map((p) => p.row + 1)));
	const later = $derived(items.filter((i) => i.daysUntilDue > hi).length);

	const ticks = $derived(Array.from({ length: (hi - lo) / 30 + 1 }, (_, i) => lo + i * 30));
	const today = $derived(x(0));
	const windowEnd = $derived(x(windowDays));

	const tickLabel = (days: number) => (days === 0 ? 'Today' : days > 0 ? `+${days}` : `−${-days}`);
</script>

<figure class="strip" aria-label="Due dates around today, from the most overdue on the left to later ones on the right">
	<div class="band" style:--rows={rows}>
		<div class="zone hot" style:left="0%" style:width="{today}%">
			{#if today > 12}<span>Overdue</span>{/if}
		</div>
		<div class="zone warm" style:left="{today}%" style:width="{windowEnd - today}%">
			{#if windowEnd - today > 9}<span>Due soon</span>{/if}
		</div>
		<div class="zone cool" style:left="{windowEnd}%" style:width="{100 - windowEnd}%">
			{#if 100 - windowEnd > 9}<span>OK</span>{/if}
		</div>

		<div class="today" style:left="{today}%" aria-hidden="true"></div>

		<ol class="markers">
			{#each placed as { item, left, row }, i (item.installationId)}
				<li style:left="{left}%" style:--row={row} style:--delay="{i * 45}ms">
					<a
						class="marker {item.status}"
						class:flip={left > 62}
						href="/installations/{item.installationId}"
						aria-label="{item.customer}, {item.city}: {relativeDue(item.daysUntilDue).toLowerCase()}"
					>
						<span class="shape" aria-hidden="true"></span>
						<span class="tip" aria-hidden="true">
							<strong>{item.customer}</strong>
							<span>{item.city}, {relativeDue(item.daysUntilDue).toLowerCase()}</span>
						</span>
					</a>
				</li>
			{/each}
		</ol>
	</div>

	<div class="axis" aria-hidden="true">
		{#each ticks as tick (tick)}
			<span class:today-tick={tick === 0} class:minor={tick % 60 !== 0} style:left="{x(tick)}%">{tickLabel(tick)}</span>
		{/each}
	</div>

	<figcaption class="small muted">
		<span class="legend" aria-hidden="true">
			<StatusLabel status="Overdue" />
			<StatusLabel status="DueSoon" />
			<StatusLabel status="Ok" />
		</span>
		Days from today. Each mark is an installation; select one to open it.
		{#if later > 0}{later} more {later === 1 ? 'is' : 'are'} due after {hi} days.{/if}
	</figcaption>
</figure>

<style>
	.strip {
		container-type: inline-size;
		margin: 0;
		padding: var(--space-4) var(--space-4) var(--space-3);
		background: var(--surface);
		border: 1px solid var(--rule);
		border-radius: var(--radius);
	}

	/* Zone names label the band when it's wide; on a phone a legend under it takes over. */
	.legend {
		display: none;
		flex-wrap: wrap;
		gap: var(--space-1) var(--space-4);
		margin-bottom: var(--space-2);
		font-size: var(--step-0);
	}

	@container (max-width: 40rem) {
		.zone span {
			display: none;
		}

		.legend {
			display: flex;
		}

		/* No zone names to clear, so the markers can sit higher. */
		.band {
			height: calc(1.25rem + var(--rows) * 1.5rem);
		}

		.markers li {
			top: calc(0.75rem + var(--row) * 1.5rem);
		}
	}

	.band {
		position: relative;
		height: calc(2.25rem + var(--rows) * 1.5rem);
		border-radius: 2px;
		overflow: visible;
	}

	.zone {
		position: absolute;
		top: 0;
		bottom: 0;
	}

	.zone span {
		position: absolute;
		top: 0.35rem;
		left: 0.5rem;
		font-size: var(--step--1);
		font-weight: 650;
		font-stretch: var(--condensed);
		white-space: nowrap;
	}

	/* Hotter the further past due, cooler the further away: the gradient is the urgency. */
	.zone.hot {
		background: linear-gradient(to right, color-mix(in srgb, var(--hot-mark) 30%, var(--hot-tint)), var(--hot-tint));
		color: var(--hot);
		border-radius: 2px 0 0 2px;
	}

	.zone.warm {
		background: var(--warm-tint);
		color: var(--warm);
	}

	.zone.cool {
		background: linear-gradient(to right, var(--cool-tint), color-mix(in srgb, var(--cool-mark) 22%, var(--cool-tint)));
		color: var(--cool);
		border-radius: 0 2px 2px 0;
	}

	.today {
		position: absolute;
		top: -0.35rem;
		bottom: -0.35rem;
		width: 2px;
		margin-left: -1px;
		background: var(--ink);
	}

	.markers {
		list-style: none;
		margin: 0;
		padding: 0;
	}

	.markers li {
		position: absolute;
		top: calc(1.9rem + var(--row) * 1.5rem);
		width: 0;
		animation: settle 420ms cubic-bezier(0.2, 0.7, 0.3, 1) both;
		animation-delay: var(--delay);
	}

	@keyframes settle {
		from {
			opacity: 0;
			transform: translateY(-0.6rem);
		}
	}

	/* The link is a plain square hit area; the clipped shape and the tooltip are separate children,
	   so clipping the shape never clips the tooltip. */
	.marker {
		position: absolute;
		left: -0.7rem;
		top: -0.1rem;
		display: block;
		width: 1.4rem;
		height: 1.4rem;
		border-radius: 50%;
	}

	.shape {
		position: absolute;
		inset: 0.1rem;
		background: currentColor;
	}

	.marker.Overdue {
		color: var(--hot-mark);
	}

	.marker.Overdue .shape {
		clip-path: polygon(50% 0, 100% 100%, 0 100%);
	}

	.marker.DueSoon {
		color: var(--warm-mark);
	}

	.marker.DueSoon .shape {
		clip-path: polygon(50% 0, 100% 50%, 50% 100%, 0 50%);
	}

	.marker.Ok {
		color: var(--cool-mark);
	}

	.marker.Ok .shape {
		border-radius: 50%;
		inset: 0.18rem;
	}

	.markers li:has(.marker:hover, .marker:focus-visible) {
		z-index: 2;
	}

	.marker:hover,
	.marker:focus-visible {
		outline: 2px solid var(--ink);
		outline-offset: 2px;
	}

	.tip {
		display: none;
		position: absolute;
		bottom: calc(100% + 0.55rem);
		left: -0.4rem;
		padding: var(--space-2) var(--space-3);
		background: var(--ink);
		color: var(--paper);
		border-radius: var(--radius);
		font-size: var(--step--1);
		line-height: 1.3;
		white-space: nowrap;
		pointer-events: none;
	}

	.marker.flip .tip {
		left: auto;
		right: -0.4rem;
	}

	.tip strong,
	.tip span {
		display: block;
	}

	.marker:hover .tip,
	.marker:focus-visible .tip {
		display: block;
	}

	.axis {
		position: relative;
		height: 1.6rem;
		margin-top: 0.45rem;
		font-size: var(--step--1);
		font-stretch: var(--condensed);
		color: var(--ink-2);
	}

	.axis span {
		position: absolute;
		transform: translateX(-50%);
		white-space: nowrap;
	}

	.axis span:first-child {
		transform: none;
	}

	.axis span:last-child {
		transform: translateX(-100%);
	}

	.axis .today-tick {
		color: var(--ink);
		font-weight: 700;
	}

	@media (max-width: 44rem) {
		.axis .minor:not(.today-tick) {
			display: none;
		}
	}

	figcaption {
		margin-top: var(--space-2);
	}
</style>
