"use client";

import { useSearchParams } from "next/navigation";
import { useState } from "react";
import { Aviso, Boton, Campo } from "@/components/ui";
import { inicio, type Rol } from "@/lib/menu";
import { recordarAcceso } from "@/lib/acceso";

type Respuesta = { codigo?: string };

function mensaje(estado: number, r: Respuesta): string {
  if (r.codigo === "credenciales") return "Usuario o contraseña incorrectos, vuelva a intentarlo.";
  if (r.codigo === "bloqueada") return "Número de intentos excedido, la cuenta ha sido bloqueada temporalmente.";
  if (estado === 429) return "Demasiados intentos. Espera un minuto.";
  return "No se pudo iniciar sesión. Intenta de nuevo.";
}

// egresados: portal de egresados (Figma 08), que entra con el correo personal.
export function FormLogin({ egresados = false }: { egresados?: boolean }) {
  const params = useSearchParams();
  const sesionTerminada = params.get("sesion") === "terminada";
  const contrasenaActualizada = params.get("contrasena") === "actualizada";
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function entrar(datos: FormData) {
    setEnviando(true);
    setError(null);
    try {
      const res = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ correo: datos.get("correo"), contrasena: datos.get("contrasena"), portalEgresados: egresados }),
      });
      if (res.ok) {
        // Recarga completa: con cacheComponents, router.replace dejaría este formulario oculto
        // en memoria, con la contraseña escrita y el botón deshabilitado.
        const { rol } = (await res.json()) as { rol: Rol };
        recordarAcceso(egresados);
        window.location.replace(inicio(rol));
        return;
      }
      setError(mensaje(res.status, await res.json().catch(() => ({}))));
    } catch {
      setError(mensaje(0, {}));
    }
    setEnviando(false);
  }

  return (
    <form action={entrar} className="flex flex-col gap-6 px-5 pt-6 pb-5">
      <Campo etiqueta={egresados ? "Correo personal:" : "Usuario:"} id="correo" name="correo" type="email" autoComplete="username"
        placeholder={egresados ? "Ingresa tu correo personal" : "Ingresa tu usuario"} required maxLength={100} />
      <Campo etiqueta="Contraseña:" id="contrasena" name="contrasena" type="password" autoComplete="current-password" placeholder="Ingresa tu contraseña" required maxLength={128} />
      <div className="flex justify-center pt-2">
        <Boton type="submit" disabled={enviando}>Iniciar sesión</Boton>
      </div>
      {error ? (
        <Aviso>{error}</Aviso>
      ) : contrasenaActualizada ? (
        <Aviso tipo="exito">Contraseña actualizada. Inicia sesión con tu nueva contraseña.</Aviso>
      ) : (
        sesionTerminada && <Aviso tipo="info">Tu sesión terminó. Inicia sesión de nuevo.</Aviso>
      )}
    </form>
  );
}
