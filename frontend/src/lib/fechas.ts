// El backend guarda fechas y horas en UTC y las manda sin zona («2026-11-09T22:26:29»).
// Sin la «Z», el navegador las tomaría como hora local y saldrían corridas.
export const desdeUtc = (iso: string) => new Date(/[zZ]|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`);

const dosDigitos = (n: number) => String(n).padStart(2, "0");
export const fechaLocal = (d: Date) => `${dosDigitos(d.getDate())}/${dosDigitos(d.getMonth() + 1)}/${d.getFullYear()}`;
export const horaLocal = (d: Date) => `${dosDigitos(d.getHours())}:${dosDigitos(d.getMinutes())}`;

// «Hoy, 09:14», «Ayer, 18:02» o «02/11/2026» (Figma U1).
export function haceCuanto(iso: string | null) {
  if (!iso) return "—";
  const d = desdeUtc(iso);
  const hoy = new Date();
  const ayer = new Date(hoy.getFullYear(), hoy.getMonth(), hoy.getDate() - 1);
  if (d.toDateString() === hoy.toDateString()) return `Hoy, ${horaLocal(d)}`;
  if (d.toDateString() === ayer.toDateString()) return `Ayer, ${horaLocal(d)}`;
  return fechaLocal(d);
}
