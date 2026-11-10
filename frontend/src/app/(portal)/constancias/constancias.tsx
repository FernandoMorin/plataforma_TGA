"use client";

import { useEffect, useState, type FormEvent } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, Insignia } from "@/components/modulo";
import { enviarFormulario, pdfInvalido, ZonaCarga } from "@/components/carga";
import { sesionVencida } from "@/lib/acceso";
import { desdeUtc, fechaLocal } from "@/lib/fechas";

// Constancias del egresado (Figma G5): sube el PDF y el coordinador lo marca como revisado.

type Constancia = { id: number; nombre: string | null; fecha: string | null; revisada: boolean };

export function Constancias() {
  const [constancias, setConstancias] = useState<Constancia[] | null>(null);
  const [version, setVersion] = useState(0);
  const [archivo, setArchivo] = useState<File | null>(null);
  const [aviso, setAviso] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch("/api/egresado/constancias").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setConstancias(await res.json());
    });
  }, [version]);

  function elegir(f: File | undefined) {
    setAviso(null);
    if (!f) return setArchivo(null);
    const invalido = pdfInvalido(f);
    if (invalido) return setAviso({ tipo: "error", texto: invalido });
    setArchivo(f);
  }

  async function subir(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!archivo) return setAviso({ tipo: "error", texto: "Selecciona un archivo PDF." });
    const datos = new FormData();
    datos.set("archivo", archivo);
    setEnviando(true);
    const fallo = await enviarFormulario("/api/egresado/constancias", datos);
    setEnviando(false);
    if (fallo) return setAviso({ tipo: "error", texto: fallo });
    setAviso({ tipo: "exito", texto: `Se subió ${archivo.name}.` });
    setArchivo(null);
    setVersion((v) => v + 1);
  }

  return (
    <>
      <EncabezadoPagina titulo="Constancias" descripcion="Documentos que respaldan tu situación laboral." />
      <div className="mx-auto flex w-[760px] flex-col gap-5">
        <form onSubmit={subir} className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <h2 className="text-[17px] font-semibold text-portal-titulo">Subir constancia</h2>
          <ZonaCarga archivo={archivo} onElegir={elegir} />
          {aviso && <Alerta tipo={aviso.tipo}>{aviso.texto}</Alerta>}
          <div className="flex justify-end">
            <BotonPortal type="submit" disabled={enviando}>Subir</BotonPortal>
          </div>
        </form>
        {!!constancias?.length && (
          <section className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
            <h2 className="text-[17px] font-semibold text-portal-titulo">Mis constancias</h2>
            <ul>
              {constancias.map((c) => (
                <li key={c.id} className="flex items-center gap-3 border-t border-portal-borde py-2.5">
                  <a href={`/api/documentos/${c.id}`} className="flex-1 truncate text-sm font-semibold text-portal-activo">{c.nombre}</a>
                  <span className="text-[13px] text-portal-subtexto">{c.fecha ? fechaLocal(desdeUtc(c.fecha)) : "—"}</span>
                  <Insignia tono={c.revisada ? "verde" : "gris"}>{c.revisada ? "Revisada" : "Por revisar"}</Insignia>
                </li>
              ))}
            </ul>
          </section>
        )}
      </div>
    </>
  );
}
