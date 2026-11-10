"use client";

import Link from "next/link";
import { useState, useSyncExternalStore } from "react";
import { Aviso, Boton, Campo } from "@/components/ui";
import { irAlLogin, loginDe } from "@/lib/acceso";

// El token del enlace (#token=...) se lee una sola vez y se quita de la barra de direcciones,
// para que no quede en el historial ni se comparta por error.
let tokenCapturado: string | null | undefined;
function leerToken() {
  if (tokenCapturado === undefined) {
    tokenCapturado = new URLSearchParams(window.location.hash.slice(1)).get("token");
    if (tokenCapturado) history.replaceState(null, "", window.location.pathname);
  }
  return tokenCapturado;
}
const sinSuscripcion = () => () => {};

// Sin token: pide el correo y manda el enlace (05). Con el token del enlace: nueva contraseña (06).
export function Recuperar() {
  const token = useSyncExternalStore(sinSuscripcion, leerToken, () => null);
  const [aviso, setAviso] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function enviar(ruta: string, cuerpo: unknown) {
    setEnviando(true);
    setAviso(null);
    try {
      const res = await fetch(ruta, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(cuerpo) });
      const r = await res.json().catch(() => ({}));
      if (res.ok) return r;
      setAviso({ tipo: "error", texto: res.status === 429 ? "Demasiados intentos. Espera unos minutos." : r.mensaje ?? "No se pudo completar. Intenta de nuevo." });
    } catch {
      setAviso({ tipo: "error", texto: "No se pudo completar. Intenta de nuevo." });
    } finally {
      setEnviando(false);
    }
    return null;
  }

  async function pedirEnlace(datos: FormData) {
    const r = await enviar("/api/auth/recuperar", { correo: datos.get("correo") });
    if (r) setAviso({ tipo: "exito", texto: r.mensaje });
  }

  async function guardar(datos: FormData) {
    if (datos.get("contrasena") !== datos.get("confirmar")) return setAviso({ tipo: "error", texto: "Las contraseñas no coinciden." });
    // restablecer responde 204 sin cuerpo; enviar() devuelve {} en ese caso.
    const r = await enviar("/api/auth/restablecer", { token, contrasena: datos.get("contrasena") });
    if (r) irAlLogin("contrasena=actualizada");
  }

  const volver = <Link href={loginDe()} className="text-vinculo underline">Volver al inicio de sesión</Link>;
  const avisoUi = aviso && <Aviso tipo={aviso.tipo}>{aviso.texto}</Aviso>;

  return (
    <div className="flex flex-col gap-6 px-5 pt-6 pb-5">
      <p>Recibe por correo un enlace para restablecer tu contraseña.</p>
      {token ? (
        <form action={guardar} className="flex flex-col gap-6">
          <Campo etiqueta="Nueva contraseña (12 a 128 caracteres, letras y números)" id="contrasena" name="contrasena" type="password"
            autoComplete="new-password" placeholder="Ingresa tu nueva contraseña" required minLength={12} maxLength={128} />
          <Campo etiqueta="Confirmar contraseña" id="confirmar" name="confirmar" type="password"
            autoComplete="new-password" placeholder="Repite tu nueva contraseña" required maxLength={128} />
          <div className="flex justify-center pt-2">
            <Boton type="submit" disabled={enviando}>Guardar contraseña</Boton>
          </div>
          {avisoUi}
        </form>
      ) : (
        <form action={pedirEnlace} className="flex flex-col gap-6">
          <Campo etiqueta="Correo electrónico" id="correo" name="correo" type="email" autoComplete="email" placeholder="Ingresa tu correo" required maxLength={100} />
          <div className="flex justify-center pt-2">
            <Boton type="submit" disabled={enviando}>Enviar instrucciones</Boton>
          </div>
          {avisoUi}
        </form>
      )}
      {volver}
    </div>
  );
}
