// The shapes the ImpiantiTracker API returns (see src/ImpiantiTracker.Api/Contracts.cs).
// Dates arrive as "yyyy-mm-dd" strings and stay strings: turning them into Date objects
// would bring time zones into values that have none.

export type IsoDate = string;

export type InstallationType = 'Boiler' | 'HeatPump' | 'AirConditioner' | 'PelletStove' | 'SolarThermal';
export type VisitKind = 'Maintenance' | 'EfficiencyCheck' | 'Repair';
export type MaintenanceStatus = 'Ok' | 'DueSoon' | 'Overdue';

export interface DueItem {
	installationId: number;
	customer: string;
	city: string;
	phone: string | null;
	type: InstallationType;
	brand: string;
	lastService: IsoDate | null;
	nextDue: IsoDate;
	status: MaintenanceStatus;
	daysUntilDue: number;
}

export interface MaintenancePlan {
	asOf: IsoDate;
	warningDays: number;
	items: DueItem[];
}

export interface InstallationRow {
	id: number;
	customer: string;
	city: string;
	type: InstallationType;
	brand: string;
	model: string | null;
	powerKw: number;
	installedOn: IsoDate;
	lastVisit: IsoDate | null;
}

export interface Customer {
	id: number;
	name: string;
	address: string;
	city: string;
	phone: string | null;
	email: string | null;
}

export interface Visit {
	id: number;
	date: IsoDate;
	kind: VisitKind;
	technician: string;
	cost: number;
	notes: string | null;
}

export interface InstallationDetail {
	id: number;
	type: InstallationType;
	brand: string;
	model: string | null;
	serialNumber: string | null;
	powerKw: number;
	installedOn: IsoDate;
	maintenanceIntervalMonths: number;
	registryCode: string | null;
	customer: Customer;
	due: DueItem;
	visits: Visit[];
}

export interface VisitCreated {
	visitId: number;
	due: DueItem;
}

export interface YearReport {
	year: number;
	totalVisits: number;
	totalRevenue: number;
	byMonth: { month: number; visits: number; revenue: number }[];
	byTechnician: { technician: string; visits: number; revenue: number }[];
	byKind: { kind: VisitKind; visits: number }[];
}

/** RFC 9457 problem details, as the API returns them for 400 and 404 responses. */
export interface Problem {
	title?: string;
	status?: number;
	detail?: string;
	errors?: Record<string, string[]>;
}
