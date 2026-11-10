"use client";

import { useEffect, useState, type FormEvent } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, AreaTexto, BotonPortal, Dato, Dialogo } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { sesionVencida } from "@/lib/acceso";
import { pagina } from "@/lib/menu";
import { fecha } from "../../alumnos/[clave]/pestanas";

// Solicitudes de dirección (Figma PT1) y su rechazo (PT8).

type Solicitud = {
  id: number;
  clave: number;
  alumno: string;
  generacion: string | null;
  fecha: string | null;
  modalidad: string | null;
  titulo: string | null;
  linea: string | null;
};

export function Solicitudes() {
  const [lista, setLista] = useState<Solicitud[] | null>(null);
  const [version, setVersion] = useState(0);
  const [rechazando, setRechazando] = useState<Solicitud | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState<number | null>(null);

  useEffect(() => {
    fetch("/api/profesor/solicitudes").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setLista(await res.json());
    });
  }, [version]);

  async function aceptar(s: Solicitud) {
    setEnviando(s.id);
    const fallo = await enviarJson(`/api/profesor/solicitudes/${s.id}/aceptar`, "POST", {});
    setEnviando(null);
    setError(fallo);
    setVersion((v) => v + 1);
  }

  const p = pagina("/profesor/solicitudes")!;
  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion={p.descripcion} />
      <div className="mx-auto flex w-[860px] flex-col gap-5">
        {error && <Alerta tipo="error">{error}</Alerta>}
        {lista?.length === 0 && <p className="text-[15px] text-portal-subtexto">No tienes solicitudes pendientes.</p>}
        {lista?.map((s) => (
          <section key={s.id} className="flex flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-6 py-5">
            <div className="flex items-center justify-between">
              <div className="flex flex-col gap-[3px]">
                <h2 className="text-sm font-semibold text-portal-titulo">{s.alumno}</h2>
                <p className="text-[13px] text-portal-subtexto">{s.clave}{s.generacion && ` · Generación ${s.generacion}`}</p>
              </div>
              <p className="text-[13px] text-portal-subtexto">{fecha(s.fecha)}</p>
            </div>
            <dl className="flex flex-col gap-2">
              <Dato etiqueta="Modalidad">{s.modalidad}</Dato>
              <Dato etiqueta="Título">{s.titulo}</Dato>
              <Dato etiqueta="Línea de investigación">{s.linea}</Dato>
            </dl>
            <div className="flex justify-end gap-2.5">
              <BotonPortal variante="contorno" onClick={() => setRechazando(s)} disabled={enviando === s.id}>Rechazar</BotonPortal>
              <BotonPortal onClick={() => aceptar(s)} disabled={enviando === s.id}>Aceptar</BotonPortal>
            </div>
          </section>
        ))}
      </div>
      <DialogoRechazo solicitud={rechazando} onCerrar={() => setRechazando(null)} onListo={() => { setRechazando(null); setVersion((v) => v + 1); }} />
    </>
  );
}

function DialogoRechazo({ solicitud, onCerrar, onListo }: { solicitud: Solicitud | null; onCerrar: () => void; onListo: () => void }) {
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function rechazar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setEnviando(true);
    const fallo = await enviarJson(`/api/profesor/solicitudes/${solicitud!.id}/rechazar`, "POST", { motivo: new FormData(e.currentTarget).get("motivo") });
    setEnviando(false);
    if (fallo) setError(fallo);
    else { setError(null); onListo(); }
  }

  return (
    <Dialogo abierto={solicitud !== null} onCerrar={() => { setError(null); onCerrar(); }} titulo="Rechazar solicitud"
      subtitulo={solicitud ? `${solicitud.alumno} · ${solicitud.clave}` : undefined} ancho="w-[520px]">
      <form onSubmit={rechazar} className="flex flex-col gap-[18px]">
        <AreaTexto etiqueta="Motivo" name="motivo" required maxLength={255} />
        {error && <Alerta tipo="error">{error}</Alerta>}
        <div className="flex justify-end gap-2.5">
          <BotonPortal type="button" variante="contorno" onClick={onCerrar}>Cancelar</BotonPortal>
          <BotonPortal type="submit" disabled={enviando}>Rechazar</BotonPortal>
        </div>
      </form>
    </Dialogo>
  );
}
