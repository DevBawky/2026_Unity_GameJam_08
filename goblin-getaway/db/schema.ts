import { sqliteTable, text, integer } from 'drizzle-orm/sqlite-core';
export const rooms = sqliteTable('rooms', {
  code: text('code').primaryKey(), host: text('host').notNull(), guest: text('guest'),
  guest3: text('guest3'), guest4: text('guest4'),
  captain: text('captain'), seen1: integer('seen1'),
  seen2: integer('seen2'), seen3: integer('seen3'), seen4: integer('seen4'),
  state: text('state').notNull(), clock: integer('clock').notNull(), rev: integer('rev').notNull(),
  expires: integer('expires').notNull()
});
