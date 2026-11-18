// Existing six-character invitations remain usable until their rooms expire.
export const isRoomNumber=value=>typeof value==='string'&&/^[1-9][0-9]{2}$/.test(value);
export const isLegacyRoomCode=value=>typeof value==='string'&&/^[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{6}$/.test(value);
