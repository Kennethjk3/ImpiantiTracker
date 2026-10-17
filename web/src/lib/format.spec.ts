import { describe, expect, it } from 'vitest';
import {
	addDays,
	formatDate,
	formatKw,
	formatLongDate,
	formatMoney,
	monthName,
	parseEuro,
	parseIsoDate,
	parseWindow,
	relativeDue,
	telHref
} from './format.ts';

// Intl puts a non-breaking space before the euro sign; compare with ordinary spaces.
const spaces = (s: string) => s.replace(/ | /g, ' ');

describe('dates', () => {
	it('reads yyyy-mm-dd without time zones', () => {
		expect(parseIsoDate('2026-10-17')).toEqual({ year: 2026, month: 10, day: 17 });
	});

	it('rejects anything else', () => {
		expect(() => parseIsoDate('17/10/2026')).toThrow();
	});

	it('formats the Italian way', () => {
		expect(formatDate('2026-03-05')).toBe('05/03/2026');
	});

	it('says Never for a missing date unless told otherwise', () => {
		expect(formatDate(null)).toBe('Never');
		expect(formatDate(undefined, '-')).toBe('-');
	});

	it('writes the long form with the right weekday', () => {
		expect(formatLongDate('2026-10-17')).toBe('Saturday 17 October 2026');
		expect(formatLongDate('2028-02-29')).toBe('Tuesday 29 February 2028');
	});

	it.each([
		['2026-10-17', -61, '2026-08-17'],
		['2026-10-17', 365, '2027-10-17'],
		['2026-03-28', 2, '2026-03-30'], // across the switch to summer time in Italy
		['2028-02-28', 1, '2028-02-29']
	])('%s plus %d days is %s', (iso, days, expected) => {
		expect(addDays(iso, days)).toBe(expected);
	});

	it('names months', () => {
		expect(monthName(1)).toBe('January');
		expect(monthName(12, true)).toBe('Dec');
		expect(() => monthName(13)).toThrow();
	});
});

describe('numbers', () => {
	it('formats euro amounts the Italian way', () => {
		expect(spaces(formatMoney(1234.5))).toBe('1.234,50 €');
	});

	it('formats power with one decimal', () => {
		expect(formatKw(16)).toBe('16,0');
		expect(formatKw(3.5)).toBe('3,5');
	});

	it.each([
		['80', 80],
		['80,50', 80.5],
		['80.50', 80.5],
		['1.234,50', 1234.5],
		['€ 90', 90],
		['', 0]
	])('reads "%s" as %d euro', (text, expected) => {
		expect(parseEuro(text)).toBe(expected);
	});

	it.each(['abc', '12,345', '1,2,3'])('refuses "%s"', (text) => {
		expect(parseEuro(text)).toBeNull();
	});
});

describe('due dates in words', () => {
	it.each([
		[-61, '61 days late'],
		[-1, '1 day late'],
		[0, 'Due today'],
		[1, 'In 1 day'],
		[14, 'In 14 days']
	])('%d → %s', (days, words) => {
		expect(relativeDue(days)).toBe(words);
	});
});

describe('the look-ahead window', () => {
	it('accepts the offered windows', () => {
		expect(parseWindow('60')).toBe(60);
	});

	it.each([null, '', '45', '-7', 'abc'])('falls back to 30 for %s', (value) => {
		expect(parseWindow(value)).toBe(30);
	});
});

describe('phone links', () => {
	it('adds the Italian prefix and drops spaces', () => {
		expect(telHref('0961 000 104')).toBe('tel:+390961000104');
	});

	it('keeps an existing international prefix', () => {
		expect(telHref('+39 351 000 0000')).toBe('tel:+393510000000');
	});
});
