// Por qué login entró el usuario (general o portal de egresados), para regresarlo al mismo al salir,
// al cambiar su contraseña o cuando vence su sesión. Solo es una preferencia del navegador:
// si no hay almacenamiento disponible, se usa el login general.
const CLAVE = "tga.acceso";

export function recordarAcceso(egresados: boolean) {
  try {
    localStorage.setItem(CLAVE, egresados ? "egresados" : "general");
  } catch {}
}

export function loginDe() {
  try {
    return localStorage.getItem(CLAVE) === "egresados" ? "/acceso-egresados" : "/";
  } catch {
    return "/";
  }
}

// Recarga completa: con cacheComponents, el router dejaría en memoria las páginas del usuario.
export const irAlLogin = (aviso?: "sesion=terminada" | "contrasena=actualizada") =>
  window.location.replace(aviso ? `${loginDe()}?${aviso}` : loginDe());

export const sesionVencida = () => irAlLogin("sesion=terminada");
