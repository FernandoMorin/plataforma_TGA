"use client";

import { useState, type FormEvent, type ReactNode } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, CampoPortal, Insignia } from "@/components/modulo";
import { enviarJson, FormContrasena, useCuenta, type Cuenta } from "@/components/cuenta";

// Mi cuenta (Figma P4 para el alumno; MC1-MC4 para profesor, coordinador, administrador y egresado).
export function MiCuenta() {
  const cuenta = useCuenta();
  if (!cuenta) return null;
  return (
    <>
      <EncabezadoPagina titulo="Mi cuenta" descripcion={`${cuenta.correo} · ${cuenta.rol}`} />
      <div className="grid grid-cols-2 items-start gap-5">
        {cuenta.rol === "Alumno" ? <DatosContacto cuenta={cuenta} /> : <DatosCuenta cuenta={cuenta} />}
        <Tarjeta titulo="Contraseña">
          <FormContrasena />
        </Tarjeta>
      </div>
    </>
  );
}

function Tarjeta({ titulo, children }: { titulo: string; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
      <h2 className="text-lg font-semibold text-portal-titulo">{titulo}</h2>
      {children}
    </section>
  );
}

function Fila({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return (
    <div className="flex items-center gap-3 text-sm">
      <dt className="w-40 shrink-0 text-portal-subtexto">{etiqueta}</dt>
      <dd className="font-medium text-portal-titulo">{children}</dd>
    </div>
  );
}

// Datos de solo lectura: los corrige la coordinación (Usuarios) o el egresado en Mi perfil.
function DatosCuenta({ cuenta }: { cuenta: Cuenta }) {
  const egresado = cuenta.rol === "Egresado";
  const conRpe = cuenta.rol === "Profesor" || cuenta.rol === "Coordinador";
  return (
    <Tarjeta titulo="Datos de la cuenta">
      <dl className="flex flex-col gap-3">
        {!egresado && <Fila etiqueta="Nombre">{cuenta.nombre ?? "—"}</Fila>}
        {conRpe && <Fila etiqueta="RPE">{cuenta.claveInstitucional ?? "—"}</Fila>}
        <Fila etiqueta={egresado ? "Correo de acceso" : "Correo"}>{cuenta.correo}</Fila>
        {egresado && <Fila etiqueta="Identificador">{cuenta.alumno?.identificadorEgresado ?? cuenta.claveInstitucional ?? "—"}</Fila>}
        <Fila etiqueta="Rol"><Insignia tono="gris">{cuenta.rol}</Insignia></Fila>
      </dl>
      {cuenta.rol === "Profesor" && <p className="text-[13px] text-portal-placeholder">Para corregir estos datos, contacta a la coordinación.</p>}
    </Tarjeta>
  );
}

function DatosContacto({ cuenta }: { cuenta: Cuenta }) {
  const [estado, setEstado] = useState<{ error?: string; guardado?: boolean }>({});
  const [enviando, setEnviando] = useState(false);

  async function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setEnviando(true);
    const error = await enviarJson("/api/cuenta/contacto", "PUT", { correoPersonal: f.get("correoPersonal"), telefono: f.get("telefono") });
    setEnviando(false);
    setEstado(error ? { error } : { guardado: true });
  }

  return (
    <Tarjeta titulo="Datos de contacto">
      <form onSubmit={guardar} onChange={() => setEstado({})} className="flex flex-col gap-4">
        <CampoPortal etiqueta="Correo personal" name="correoPersonal" type="email" required maxLength={100} defaultValue={cuenta.alumno?.correoPersonal ?? ""} />
        <CampoPortal etiqueta="Teléfono" name="telefono" type="tel" required maxLength={20} defaultValue={cuenta.alumno?.telefono ?? ""} />
        {estado.error && <p role="alert" className="text-sm text-portal-error-texto">{estado.error}</p>}
        {estado.guardado && <Alerta tipo="exito">Cambios guardados.</Alerta>}
        <div className="flex justify-end">
          <BotonPortal type="submit" disabled={enviando}>Guardar cambios</BotonPortal>
        </div>
      </form>
    </Tarjeta>
  );
}
