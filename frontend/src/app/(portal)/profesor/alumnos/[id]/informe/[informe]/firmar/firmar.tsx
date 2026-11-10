"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { sesionVencida } from "@/lib/acceso";
import { desdeUtc, fechaLocal } from "@/lib/fechas";
import { fecha } from "@/app/(portal)/alumnos/[clave]/pestanas";

// Firmar acta (Figma PT7): el acta tal como quedará y la firma de conformidad del integrante.

type Datos = {
  alumno: string;
  informe: number;
  periodo: string | null;
  miRol: string;
  estado: "En firma" | "Cerrada";
  firmadas: number;
  integrantes: number;
  firme: boolean;
  puedoFirmar: boolean;
  encabezado: string[];
  fecha: string | null;
  parrafos: string[];
  firmas: { nombre: string; rol: string; conformidad: string | null }[];
  documento: number | null;
};

export function FirmarActa() {
  const router = useRouter();
  const { id: tesis, informe: id } = useParams<{ id: string; informe: string }>();
  const [datos, setDatos] = useState<Datos | null>(null);
  const [version, setVersion] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch(`/api/profesor/evaluaciones/${id}/acta`).then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
      else router.replace(`/profesor/alumnos/${tesis}`);
    });
  }, [id, tesis, router, version]);

  async function firmar() {
    setEnviando(true);
    const fallo = await enviarJson(`/api/profesor/evaluaciones/${id}/firmar`, "POST", {});
    setEnviando(false);
    setError(fallo);
    if (!fallo) setVersion((v) => v + 1);
  }

  const volver = <Link href={`/profesor/alumnos/${tesis}`} className="text-sm font-semibold text-portal-activo">← {datos?.alumno ?? "Volver"}</Link>;
  if (!datos) return volver;
  const cerrada = datos.estado === "Cerrada";

  return (
    <>
      {volver}
      <EncabezadoPagina titulo={cerrada ? "Acta de evaluación" : "Firmar acta"} descripcion={`${datos.alumno} · Informe ${datos.informe} · ${datos.periodo ?? ""}`} />
      <div className="flex items-start gap-5">
        <article className="flex flex-1 flex-col gap-3.5 rounded-md border border-portal-borde-campo bg-blanco px-11 py-9 text-[13px] text-portal-titulo shadow-[0_4px_14px_rgba(10,18,51,0.08)]">
          {datos.encabezado.map((t) => <p key={t} className="text-center font-bold">{t}</p>)}
          <p className="text-right">{fecha(datos.fecha)}</p>
          {datos.parrafos.map((p, i) => <p key={i} className="text-justify">{p}</p>)}
          <p className="text-center font-bold">El Comité Tutorial</p>
          <ul>
            {datos.firmas.map((f) => (
              <li key={f.nombre} className="flex gap-2.5 border-b border-portal-borde py-1.5">
                <span className="flex-1">{f.nombre}</span>
                <span className="w-[110px] text-portal-subtexto">{f.rol}</span>
                <span className={`w-[150px] text-right font-medium ${f.conformidad ? "text-portal-exito-texto" : "text-portal-placeholder"}`}>
                  {f.conformidad ? `Conforme · ${fechaLocal(desdeUtc(f.conformidad))}` : "Pendiente"}
                </span>
              </li>
            ))}
          </ul>
        </article>

        <section className="flex w-[320px] flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-[22px] py-5">
          <h2 className="text-[17px] font-semibold text-portal-titulo">Tu firma</h2>
          <p className="text-sm text-portal-subtexto">Firmas como {datos.miRol}.</p>
          <p className="text-sm font-medium text-portal-titulo">Firmas: {datos.firmadas} de {datos.integrantes}</p>
          {error && <Alerta tipo="error">{error}</Alerta>}
          {datos.puedoFirmar && (
            <BotonPortal onClick={firmar} disabled={enviando} className="justify-center">Firmar de conformidad</BotonPortal>
          )}
          {datos.firme && !cerrada && <Alerta tipo="exito">Ya firmaste el acta.</Alerta>}
          {cerrada && datos.documento && (
            <a href={`/api/documentos/${datos.documento}`} className="flex justify-center rounded-lg border border-portal-borde-campo bg-blanco py-2.5 text-[15px] font-semibold text-portal-menu-texto">
              Descargar acta
            </a>
          )}
        </section>
      </div>
    </>
  );
}
