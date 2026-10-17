import type { InstallationType, IsoDate, MaintenanceStatus, VisitKind } from './types.ts';

// Labels are in English like the rest of the app; numbers, dates and money follow Italian conventions.

const MONTHS = [
	'January', 'February', 'March', 'April', 'May', 'June',
	'July', 'August', 'September', 'October', 'November', 'December'
];
const WEEKDAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

// useGrouping 'always': Italian CLDR data leaves 4-digit amounts ungrouped (1234,50 €); the CLI writes 1.234,50 €.
const money = new Intl.NumberFormat('it-IT', { style: 'currency', currency: 'EUR', useGrouping: 'always' });
const kw = new Intl.NumberFormat('it-IT', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

/** Splits "2026-10-17" into numbers without going through Date, so no time zone can shift the day. */
export function parseIsoDate(iso: IsoDate): { year: number; month: number; day: number } {
	const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
	if (!match) throw new Error(`Not a yyyy-mm-dd date: "${iso}"`);
	return { year: Number(match[1]), month: Number(match[2]), day: Number(match[3]) };
}

/** Date arithmetic on yyyy-mm-dd strings, done in UTC so daylight-saving changes can't skip or repeat a day. */
export function addDays(iso: IsoDate, days: number): IsoDate {
	const { year, month, day } = parseIsoDate(iso);
	return new Date(Date.UTC(year, month - 1, day + days)).toISOString().slice(0, 10);
}

/** 17/10/2026, or the fallback for a missing date. */
export function formatDate(iso: IsoDate | null | undefined, fallback = 'Never'): string {
	if (!iso) return fallback;
	const { year, month, day } = parseIsoDate(iso);
	return `${String(day).padStart(2, '0')}/${String(month).padStart(2, '0')}/${year}`;
}

/** Saturday 17 October 2026 */
export function formatLongDate(iso: IsoDate): string {
	const { year, month, day } = parseIsoDate(iso);
	const weekday = WEEKDAYS[new Date(Date.UTC(year, month - 1, day)).getUTCDay()];
	return `${weekday} ${day} ${MONTHS[month - 1]} ${year}`;
}

export function monthName(month: number, short = false): string {
	const name = MONTHS[month - 1];
	if (!name) throw new Error(`No month ${month}`);
	return short ? name.slice(0, 3) : name;
}

/** 1.234,50 € */
export function formatMoney(amount: number): string {
	return money.format(amount);
}

/** 16,0 */
export function formatKw(value: number): string {
	return kw.format(value);
}

const plural = (n: number, word: string) => `${n} ${word}${n === 1 ? '' : 's'}`;

/** How far a due date is from today, in words: "61 days late", "Due today", "In 14 days". */
export function relativeDue(daysUntilDue: number): string {
	if (daysUntilDue < 0) return `${plural(-daysUntilDue, 'day')} late`;
	if (daysUntilDue === 0) return 'Due today';
	return `In ${plural(daysUntilDue, 'day')}`;
}

export const typeLabels: Record<InstallationType, string> = {
	Boiler: 'Boiler',
	HeatPump: 'Heat pump',
	AirConditioner: 'Air conditioner',
	PelletStove: 'Pellet stove',
	SolarThermal: 'Solar thermal'
};

export const kindLabels: Record<VisitKind, string> = {
	Maintenance: 'Maintenance',
	EfficiencyCheck: 'Efficiency check (RCEE)',
	Repair: 'Repair'
};

export const statusLabels: Record<MaintenanceStatus, string> = {
	Overdue: 'Overdue',
	DueSoon: 'Due soon',
	Ok: 'OK'
};

/** Italian numbers dial as written; a +39 prefix makes the link work from a foreign SIM too. */
export function telHref(phone: string): string {
	const digits = phone.replace(/[^\d+]/g, '');
	return `tel:${digits.startsWith('+') ? digits : `+39${digits}`}`;
}

/** The look-ahead windows the plan offers, in days. */
export const WINDOWS = [7, 30, 60, 90] as const;
export const DEFAULT_WINDOW = 30;

/** Reads ?days= from the URL; anything that isn't one of the offered windows falls back to 30. */
export function parseWindow(value: string | null): number {
	const days = Number(value);
	return (WINDOWS as readonly number[]).includes(days) ? days : DEFAULT_WINDOW;
}

/** Reads a euro amount typed the Italian way (80,50) or the English way (80.50). Null if it isn't a number. */
export function parseEuro(text: string): number | null {
	const cleaned = text.trim().replace(/\s|€/g, '');
	if (cleaned === '') return 0;
	// "1.234,50": dots are thousands separators when a comma marks the decimals.
	const normalized = cleaned.includes(',') ? cleaned.replace(/\./g, '').replace(',', '.') : cleaned;
	if (!/^-?\d+(\.\d{1,2})?$/.test(normalized)) return null;
	return Number(normalized);
}
