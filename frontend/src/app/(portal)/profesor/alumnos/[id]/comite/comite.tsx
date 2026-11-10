"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState, type FormEvent } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, SelectorPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { sesionVencida } from "@/lib/acceso";

// Armar comité (Figma PT3): el director elige codirector opcional y dos sinodales.

type Datos = {
  alumno: string;
  clave: number;
  director: string;
  codirector: string | null;
  sinodal1: string | null;
  sinodal2: string | null;
  profesores: { rpe: string; nombre: string; externo: boolean }[];
};

export function ArmarComite() {
  const router = useRouter();
  const id = useParams<{ id: string }>().id;
  const [datos, setDatos] = useState<Datos | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch(`/api/profesor/comite/${id}`).then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
      else router.replace("/profesor/alumnos");
    });
  }, [id, router]);

  async function registrar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = Object.fromEntries(new FormData(e.currentTarget)) as Record<string, string>;
    setEnviando(true);
    const fallo = await enviarJson(`/api/profesor/comite/${id}`, "PUT", { codirector: f.codirector || null, sinodal1: f.sinodal1, sinodal2: f.sinodal2 });
    setEnviando(false);
    if (fallo) setError(fallo);
    else router.push(`/profesor/alumnos/${id}`);
  }

  if (!datos) return <EncabezadoPagina titulo="Armar comité" />;
  const opciones = datos.profesores.map((p) => <option key={p.rpe} value={p.rpe}>{p.nombre}{p.externo ? " (externo)" : ""}</option>);
  return (
    <>
      <EncabezadoPagina titulo="Armar comité" descripcion={`${datos.alumno} · ${datos.clave}`} />
      <form onSubmit={registrar} className="mx-auto flex w-[720px] flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-6 py-5">
        <h2 className="text-[17px] font-semibold text-portal-titulo">Comité tutorial</h2>
        <p className="text-sm text-portal-subtexto">Un director, un codirector opcional y dos sinodales.</p>
        <div className="flex flex-col gap-1.5">
          <span className="text-sm font-semibold text-portal-menu-texto">Director</span>
          <p className="rounded-lg bg-portal-tabla-cabecera px-3 py-2.5 text-[15px] font-medium text-portal-titulo">{datos.director} (tú)</p>
        </div>
        <SelectorPortal etiqueta="Codirector (opcional)" name="codirector" defaultValue={datos.codirector ?? ""}>
          <option value="">Sin codirector</option>
          {opciones}
        </SelectorPortal>
        <SelectorPortal etiqueta="Sinodal 1" name="sinodal1" required defaultValue={datos.sinodal1 ?? ""}>
          <option value="" disabled>Seleccionar profesor</option>
          {opciones}
        </SelectorPortal>
        <SelectorPortal etiqueta="Sinodal 2" name="sinodal2" required defaultValue={datos.sinodal2 ?? ""}>
          <option value="" disabled>Seleccionar profesor</option>
          {opciones}
        </SelectorPortal>
        {error && <Alerta tipo="error">{error}</Alerta>}
        <div className="flex justify-end gap-2.5">
          <Link href={`/profesor/alumnos/${id}`} className="rounded-lg border border-portal-borde-campo bg-blanco py-2.5 pr-[18px] pl-4 text-[15px] font-semibold text-portal-menu-texto">Cancelar</Link>
          <BotonPortal type="submit" disabled={enviando}>Registrar comité</BotonPortal>
        </div>
      </form>
    </>
  );
}
