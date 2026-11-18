CREATE TABLE `rooms` (
	`code` text PRIMARY KEY NOT NULL,
	`host` text NOT NULL,
	`guest` text,
	`state` text NOT NULL,
	`clock` integer NOT NULL,
	`rev` integer NOT NULL,
	`expires` integer NOT NULL
);
