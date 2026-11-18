import { createClient } from '@libsql/client/web';

const secureProtocols = new Set(['libsql:', 'https:', 'wss:']);
const loopbackHosts = new Set(['localhost', '127.0.0.1', '[::1]']);

export function createRoomClient() {
  const url = process.env.TURSO_DATABASE_URL?.trim();
  const authToken = process.env.TURSO_AUTH_TOKEN?.trim();
  if (!url) return null;

  let endpoint;
  try {
    endpoint = new URL(url);
  } catch {
    return null;
  }
  const isLoopback = loopbackHosts.has(endpoint.hostname);
  const isLocalProtocol = endpoint.protocol === 'http:' || endpoint.protocol === 'ws:';
  if (!secureProtocols.has(endpoint.protocol) && !(isLoopback && isLocalProtocol)) return null;
  if (!authToken && !isLoopback) return null;

  return createClient({ url, authToken: authToken || undefined, intMode: 'number' });
}

function d1Result({ rows, rowsAffected }) {
  return { results: rows, meta: { changes: rowsAffected } };
}

let database;

export async function roomDb() {
  if (database) return database;
  const client = createRoomClient();
  if (!client) return null;

  database = {
    prepare(sql) {
      return {
        bind(...args) {
          const statement = { sql, args };
          return {
            ...statement,
            async first() {
              const result = await client.execute(statement);
              return result.rows[0] ?? null;
            },
            async run() {
              return d1Result(await client.execute(statement));
            },
          };
        },
      };
    },
    async batch(statements) {
      const queries = statements.map(({ sql, args }) => ({ sql, args }));
      const results = await client.batch(queries, 'write');
      return results.map(d1Result);
    },
  };
  return database;
}
