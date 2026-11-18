import { env } from 'cloudflare:workers';

export async function roomDb() {
  return env.DB ?? null;
}
