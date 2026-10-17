<script lang="ts">
	import { statusLabels } from '#lib/format.ts';
	import type { MaintenanceStatus } from '#lib/types.ts';

	let { status }: { status: MaintenanceStatus } = $props();
</script>

<!-- Shape and colour both encode the status (triangle, diamond, circle), so it reads without colour too. -->
<span class="status {status}">{statusLabels[status]}</span>

<style>
	.status {
		display: inline-flex;
		align-items: center;
		gap: 0.45em;
		font-weight: 650;
		font-stretch: var(--condensed);
		white-space: nowrap;
	}

	.status::before {
		content: '';
		flex: none;
		width: 0.75em;
		height: 0.75em;
		background: currentColor;
	}

	.Overdue {
		color: var(--hot);
	}

	.Overdue::before {
		clip-path: polygon(50% 0, 100% 100%, 0 100%);
	}

	.DueSoon {
		color: var(--warm);
	}

	.DueSoon::before {
		clip-path: polygon(50% 0, 100% 50%, 50% 100%, 0 50%);
	}

	.Ok {
		color: var(--cool);
	}

	.Ok::before {
		border-radius: 50%;
	}
</style>
