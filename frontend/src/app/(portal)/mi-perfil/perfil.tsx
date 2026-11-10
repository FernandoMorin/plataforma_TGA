"use client";

import { useEffect, useState, type FormEvent } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, CampoPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { fechaCorta } from "@/components/egresado";
import { sesionVencida } from "@/lib/acceso";

// Mi perfil del egresado (Figma G1). En junio se le pide revisar y confirmar sus datos.

type Perfil = {
  identificador: string | null;
  generacion: string | null;
  nombre: string;
  correo: string | null;
  telefono: string | null;
  ciudad: string | null;
  otros: string | null;
  actualizado: string | null;
  debeActualizar: boolean;
};

export function MiPerfil() {
  const [perfil, setPerfil] = useState<Perfil | null>(null);
  const [version, setVersion] = useState(0);
  const [aviso, setAviso] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch("/api/egresado/perfil").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setPerfil(await res.json());
    });
  }, [version]);

  async function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = Object.fromEntries(new FormData(e.currentTarget)) as Record<string, string>;
    setEnviando(true);
    const fallo = await enviarJson("/api/egresado/perfil", "PUT", f);
    setEnviando(false);
    setAviso(fallo ? { tipo: "error", texto: fallo } : { tipo: "exito", texto: "Datos guardados." });
    if (!fallo) setVersion((v) => v + 1);
  }

  async function confirmar() {
    setEnviando(true);
    const fallo = await enviarJson("/api/egresado/perfil/confirmar", "POST", {});
    setEnviando(false);
    setAviso(fallo ? { tipo: "error", texto: fallo } : { tipo: "exito", texto: "Datos confirmados." });
    if (!fallo) setVersion((v) => v + 1);
  }

  if (!perfil) return <EncabezadoPagina titulo="Mi perfil" />;
  return (
    <>
      <EncabezadoPagina titulo="Mi perfil" descripcion={`${perfil.identificador} · Generación ${perfil.generacion}`} />
      <div className="mx-auto flex w-[760px] flex-col gap-5">
        {perfil.debeActualizar && <Alerta tipo="advertencia">Junio es el mes de actualización. Revisa tus datos y confírmalos.</Alerta>}
        {aviso && <Alerta tipo={aviso.tipo}>{aviso.texto}</Alerta>}
        <form key={version} onSubmit={guardar} className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <div className="flex items-center justify-between">
            <h2 className="text-[17px] font-semibold text-portal-titulo">Datos personales</h2>
            <p className="text-[13px] text-portal-subtexto">Actualizado el {fechaCorta(perfil.actualizado)}</p>
          </div>
          <div className="grid grid-cols-2 gap-x-5 gap-y-4">
            <div className="flex flex-col gap-1.5">
              <span className="text-sm font-semibold text-portal-menu-texto">Nombre</span>
              <p className="rounded-lg bg-portal-tabla-cabecera px-3 py-2.5 text-[15px] font-medium text-portal-titulo">{perfil.nombre}</p>
            </div>
            <CampoPortal etiqueta="Correo" name="correo" type="email" required maxLength={100} defaultValue={perfil.correo ?? ""} />
            <CampoPortal etiqueta="Teléfono" name="telefono" type="tel" maxLength={20} defaultValue={perfil.telefono ?? ""} />
            <CampoPortal etiqueta="Ciudad" name="ciudad" maxLength={100} defaultValue={perfil.ciudad ?? ""} />
            <div className="col-span-2">
              <CampoPortal etiqueta="Otros medios de contacto" name="otros" maxLength={255} defaultValue={perfil.otros ?? ""} />
            </div>
          </div>
          <div className="flex justify-end gap-2.5">
            <BotonPortal type="button" variante="contorno" onClick={confirmar} disabled={enviando}>Confirmar sin cambios</BotonPortal>
            <BotonPortal type="submit" disabled={enviando}>Guardar cambios</BotonPortal>
          </div>
        </form>
      </div>
    </>
  );
}
