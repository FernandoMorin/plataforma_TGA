"use client";

import { useEffect, useState, type FormEvent } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, AreaTexto, BotonPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { ListaComentarios, type Comentario } from "@/components/egresado";
import { sesionVencida } from "@/lib/acceso";

// Comentarios del egresado (Figma G4): la retroalimentación para mejorar la maestría (RF-02.8).

export function Comentarios() {
  const [enviadas, setEnviadas] = useState<Comentario[] | null>(null);
  const [version, setVersion] = useState(0);
  const [aviso, setAviso] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch("/api/egresado/comentarios").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setEnviadas(await res.json());
    });
  }, [version]);

  async function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = e.currentTarget;
    const f = new FormData(form);
    setEnviando(true);
    const fallo = await enviarJson("/api/egresado/comentarios", "POST", { comentario: f.get("comentario"), sugerencia: f.get("sugerencia") });
    setEnviando(false);
    setAviso(fallo ? { tipo: "error", texto: fallo } : { tipo: "exito", texto: "Gracias, se envió tu comentario." });
    if (!fallo) { form.reset(); setVersion((v) => v + 1); }
  }

  return (
    <>
      <EncabezadoPagina titulo="Comentarios" descripcion="Ayúdanos a mejorar la maestría." />
      <div className="mx-auto flex w-[760px] flex-col gap-5">
        <form onSubmit={enviar} className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <h2 className="text-[17px] font-semibold text-portal-titulo">Nuevo comentario</h2>
          <AreaTexto etiqueta="Comentario" name="comentario" maxLength={4000} placeholder="¿Cómo te ha servido lo que aprendiste en la maestría?" />
          <AreaTexto etiqueta="Sugerencia" name="sugerencia" maxLength={4000} placeholder="¿Qué mejorarías del programa?" />
          {aviso && <Alerta tipo={aviso.tipo}>{aviso.texto}</Alerta>}
          <div className="flex justify-end">
            <BotonPortal type="submit" disabled={enviando}>Enviar</BotonPortal>
          </div>
        </form>
        {!!enviadas?.length && (
          <section className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
            <h2 className="text-[17px] font-semibold text-portal-titulo">Enviadas</h2>
            <ListaComentarios comentarios={enviadas} />
          </section>
        )}
      </div>
    </>
  );
}
