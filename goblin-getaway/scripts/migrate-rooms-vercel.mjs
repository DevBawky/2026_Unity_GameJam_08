import { fileURLToPath } from 'node:url';
import { drizzle } from 'drizzle-orm/libsql/web';
import { migrate } from 'drizzle-orm/libsql/migrator';
import { createRoomClient } from '../db/room-turso.mjs';

let client;
try {
  client = createRoomClient();
  if (!client) {
    console.error('Set TURSO_DATABASE_URL and TURSO_AUTH_TOKEN before migrating the room database.');
    process.exitCode = 1;
  } else {
    const migrationsFolder = fileURLToPath(new URL('../drizzle/', import.meta.url));
    await migrate(drizzle(client), { migrationsFolder });
    console.log('Room database migrations applied.');
  }
} catch {
  console.error('Room database migration failed. Check the Turso connection and write permissions.');
  process.exitCode = 1;
} finally {
  client?.close();
}
