"use client";

import { irAlLogin, sesionVencida } from "@/lib/acceso";

import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { Check } from "lucide-react";
import { BotonPortal, CampoPortal } from "@/components/modulo";

// Primer acceso (Figma P1), completar datos (P3) y el formulario de contraseña que comparte con Mi cuenta.

export type Cuenta = {
  nombre: string | null;
  correo: string;
  claveInstitucional: string | null;
  rol: string;
  alumno: { clave: number; correoInstitucional: string | null; correoPersonal: string | null; telefono: string | null; generacion: string | null; identificadorEgresado: string | null } | null;
};

// Al cambiar la contraseña el backend cierra todas las sesiones: se vuelve al login con el aviso de Figma 07.
const alLogin = () => irAlLogin("contrasena=actualizada");

export async function enviarJson(ruta: string, metodo: string, cuerpo: unknown): Promise<string | null> {
  try {
    const res = await fetch(ruta, { method: metodo, headers: { "Content-Type": "application/json" }, body: JSON.stringify(cuerpo) });
    if (res.status === 401) { sesionVencida(); return null; }
    if (res.ok) return null;
    return (await res.json().catch(() => ({}))).mensaje ?? "Revisa los datos e intenta de nuevo.";
  } catch {
    return "No se pudo guardar. Intenta de nuevo.";
  }
}

export function useCuenta() {
  const [cuenta, setCuenta] = useState<Cuenta | null>(null);
  useEffect(() => {
    fetch("/api/cuenta").then(async (res) => (res.ok ? setCuenta(await res.json()) : res.status === 401 && sesionVencida()));
  }, []);
  return cuenta;
}

function TarjetaCentrada({ titulo, descripcion, ancho, children }: { titulo: string; descripcion: string; ancho: string; children: ReactNode }) {
  return (
    <main className="flex flex-1 justify-center px-8 pt-16 pb-10">
      <section className={`flex h-fit flex-col gap-5 rounded-[14px] border border-portal-borde bg-blanco px-9 py-8 shadow-[0_4px_16px_rgba(10,18,51,0.06)] ${ancho}`}>
        <div className="flex flex-col gap-1.5">
          <h1 className="text-2xl font-bold text-portal-titulo">{titulo}</h1>
          <p className="text-[15px] text-portal-descripcion">{descripcion}</p>
        </div>
        {children}
      </section>
    </main>
  );
}

function Requisito({ cumple, children }: { cumple: boolean; children: string }) {
  return (
    <li className={`flex items-center gap-2 text-[13px] ${cumple ? "text-portal-exito-texto" : "text-portal-subtexto"}`}>
      {cumple ? (
        <span className="flex size-[18px] items-center justify-center rounded-full bg-portal-exito-texto text-blanco"><Check size={12} strokeWidth={3} aria-hidden /></span>
      ) : (
        <span className="size-[18px] rounded-full border-[1.5px] border-portal-borde-campo" aria-hidden />
      )}
      {children}
      <span className="sr-only">{cumple ? "(cumple)" : "(pendiente)"}</span>
    </li>
  );
}

// Formulario de cambio de contraseña. En el primer acceso muestra los requisitos en vivo (P1);
// en Mi cuenta, la regla va en el texto de ejemplo (P4).
export function FormContrasena({ primerAcceso }: { primerAcceso?: boolean }) {
  const [actual, setActual] = useState("");
  const [nueva, setNueva] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const confirmar = new FormData(e.currentTarget).get("confirmar");
    if (confirmar !== nueva) return setError("Las contraseñas no coinciden.");
    setEnviando(true);
    const err = await enviarJson("/api/cuenta/contrasena", "POST", { actual, nueva });
    setEnviando(false);
    if (err) setError(err);
    else alLogin();
  }

  return (
    <form onSubmit={guardar} className="flex flex-col gap-5">
      <CampoPortal etiqueta={primerAcceso ? "Contraseña temporal" : "Contraseña actual"} type="password" required autoComplete="current-password"
        placeholder={primerAcceso ? undefined : "Tu contraseña actual"} value={actual} onChange={(e) => setActual(e.target.value)} />
      <div className="flex flex-col gap-1.5">
        <CampoPortal etiqueta="Nueva contraseña" type="password" required minLength={12} maxLength={128} autoComplete="new-password"
          placeholder={primerAcceso ? undefined : "Mínimo 12 caracteres, con letras y números"} value={nueva} onChange={(e) => setNueva(e.target.value)} />
        {primerAcceso && (
          <ul className="flex flex-col gap-1.5" aria-label="Requisitos">
            <Requisito cumple={nueva.length >= 12}>12 caracteres o más</Requisito>
            <Requisito cumple={/\p{L}/u.test(nueva) && /\d/.test(nueva)}>Letras y números</Requisito>
            <Requisito cumple={nueva.length > 0 && nueva !== actual}>Distinta a la temporal</Requisito>
          </ul>
        )}
      </div>
      <CampoPortal etiqueta="Confirmar nueva contraseña" name="confirmar" type="password" required autoComplete="new-password" placeholder="Repite la nueva contraseña" />
      {error && <p role="alert" className="text-sm text-portal-error-texto">{error}</p>}
      {primerAcceso ? (
        <BotonPortal type="submit" disabled={enviando} className="justify-center">Cambiar contraseña</BotonPortal>
      ) : (
        <div className="flex justify-end">
          <BotonPortal type="submit" variante="contorno" disabled={enviando}>Cambiar contraseña</BotonPortal>
        </div>
      )}
    </form>
  );
}

export function PrimerAcceso({ onSalir }: { onSalir: () => void }) {
  return (
    <TarjetaCentrada titulo="Cambia tu contraseña" descripcion="Es tu primer acceso. Reemplaza la contraseña temporal por una propia." ancho="w-[520px]">
      <FormContrasena primerAcceso />
      <button onClick={onSalir} className="text-sm font-semibold text-portal-activo">Cerrar sesión</button>
    </TarjetaCentrada>
  );
}

export function CompletarDatos({ onListo }: { onListo: () => void }) {
  const cuenta = useCuenta();
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const a = cuenta?.alumno;

  async function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setEnviando(true);
    const err = await enviarJson("/api/cuenta/contacto", "PUT", { correoPersonal: f.get("correoPersonal"), telefono: f.get("telefono") });
    setEnviando(false);
    if (err) setError(err);
    else onListo();
  }

  return (
    <TarjetaCentrada titulo="Completa tus datos" descripcion="Necesitamos una forma de contactarte." ancho="w-[600px]">
      {a && (
        <dl className="flex flex-col gap-2.5 rounded-[10px] bg-portal-tabla-cabecera px-[18px] py-4 text-sm">
          {[["Nombre", cuenta.nombre], ["Clave", a.clave], ["Correo institucional", a.correoInstitucional], ["Generación", a.generacion]].map(([e, v]) => (
            <div key={e} className="flex gap-3">
              <dt className="w-40 shrink-0 text-portal-subtexto">{e}</dt>
              <dd className="font-medium text-portal-titulo">{v ?? "—"}</dd>
            </div>
          ))}
        </dl>
      )}
      <form onSubmit={guardar} className="flex flex-col gap-5">
        <CampoPortal etiqueta="Correo personal" name="correoPersonal" type="email" required maxLength={100} placeholder="nombre@ejemplo.com" defaultValue={a?.correoPersonal ?? ""} />
        <CampoPortal etiqueta="Teléfono" name="telefono" type="tel" required maxLength={20} placeholder="Ej. 444 123 4567" defaultValue={a?.telefono ?? ""} />
        {error && <p role="alert" className="text-sm text-portal-error-texto">{error}</p>}
        <BotonPortal type="submit" disabled={enviando || !cuenta} className="justify-center">Guardar y continuar</BotonPortal>
      </form>
    </TarjetaCentrada>
  );
}
