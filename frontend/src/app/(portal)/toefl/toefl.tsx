"use client";

import { useEffect, useState, type FormEvent } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, CampoPortal, Dato, Insignia } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";
import { enviarFormulario, pdfInvalido, ZonaCarga } from "@/components/carga";
import { enviarJson } from "@/components/cuenta";
import { pagina } from "@/lib/menu";
import { tonoToefl } from "../alumnos/consulta";
import { fecha } from "../alumnos/[clave]/pestanas";

// Requisito TOEFL del alumno (Figma T1 registrar, T2 en revisión; Aprobado como T2 con puntaje).
// La próxima fecha (D4) no está en Figma: mientras sea futura solo se muestra; al pasar, se pide subir
// los resultados o asignar otra fecha.

type Toefl = {
  estado: string;
  fechaPresentacion?: string | null;
  fechaProxima?: string | null;
  puntaje?: number | null;
  comprobante?: { id: number; nombre: string | null } | null;
};

export function RequisitoToefl() {
  const [toefl, setToefl] = useState<Toefl | null>(null);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    fetch("/api/toefl").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setToefl(await res.json());
    });
  }, [version]);

  const p = pagina("/toefl")!;
  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion={p.descripcion} />
      {toefl && (
        <section className="mx-auto flex w-[640px] flex-col gap-[18px] rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold text-portal-titulo">Tu examen</h2>
            <Insignia tono={tonoToefl[toefl.estado] ?? "gris"}>{toefl.estado}</Insignia>
          </div>
          {toefl.estado === "Pendiente"
            ? <Pendiente proxima={toefl.fechaProxima ?? null} onListo={() => setVersion((v) => v + 1)} />
            : <Resumen toefl={toefl} />}
        </section>
      )}
    </>
  );
}

function Resumen({ toefl }: { toefl: Toefl }) {
  return (
    <>
      <dl className="flex flex-col gap-2.5">
        <Dato etiqueta="Fecha del examen">{fecha(toefl.fechaPresentacion ?? null)}</Dato>
        {toefl.estado === "Aprobado" && <Dato etiqueta="Puntaje">{toefl.puntaje ?? "—"}</Dato>}
        <Dato etiqueta="Comprobante">
          {toefl.comprobante ? (
            <a href={`/api/documentos/${toefl.comprobante.id}`} className="font-semibold text-portal-activo">{toefl.comprobante.nombre}</a>
          ) : "—"}
        </Dato>
      </dl>
      {toefl.estado === "Presentado" && <Alerta tipo="info">En revisión por la coordinación.</Alerta>}
    </>
  );
}

const hoy = () => new Date().toLocaleDateString("en-CA");

function Pendiente({ proxima, onListo }: { proxima: string | null; onListo: () => void }) {
  const [cambiando, setCambiando] = useState(false);
  const dia = proxima?.slice(0, 10) ?? null;
  const futura = dia !== null && dia >= hoy();
  const listo = () => { setCambiando(false); onListo(); };

  if (futura && !cambiando)
    return (
      <div className="flex items-center gap-3">
        <div className="flex-1"><Alerta tipo="info">Presentarás el examen el {fecha(dia)}.</Alerta></div>
        <BotonPortal variante="contorno" onClick={() => setCambiando(true)}>Cambiar fecha</BotonPortal>
      </div>
    );
  return (
    <>
      {dia && !futura && <Alerta tipo="advertencia">Tu examen era el {fecha(dia)}. Sube tus resultados o asigna una nueva fecha.</Alerta>}
      {(!futura || cambiando) && <FechaProxima actual={futura ? dia : null} separar={!futura} onListo={listo} onCancelar={cambiando ? () => setCambiando(false) : undefined} />}
      {!futura && <Registrar onListo={onListo} />}
    </>
  );
}

function FechaProxima({ actual, separar, onListo, onCancelar }: { actual: string | null; separar: boolean; onListo: () => void; onCancelar?: () => void }) {
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setEnviando(true);
    const fallo = await enviarJson("/api/toefl/fecha-proxima", "PUT", { fecha: new FormData(e.currentTarget).get("proxima") });
    setEnviando(false);
    if (fallo) setError(fallo);
    else onListo();
  }

  return (
    <form onSubmit={guardar} className={`flex flex-col gap-3 ${separar ? "border-b border-portal-borde pb-[18px]" : ""}`}>
      <div className="flex items-end gap-3">
        <div className="w-[280px]">
          <CampoPortal etiqueta="¿Cuándo lo presentarás?" name="proxima" type="date" required min={hoy()} defaultValue={actual ?? undefined} />
        </div>
        <BotonPortal type="submit" variante="contorno" disabled={enviando}>Guardar fecha</BotonPortal>
        {onCancelar && <BotonPortal type="button" variante="secundario" onClick={onCancelar}>Cancelar</BotonPortal>}
      </div>
      {error && <Alerta tipo="error">{error}</Alerta>}
    </form>
  );
}

function Registrar({ onListo }: { onListo: () => void }) {
  const [archivo, setArchivo] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  function elegir(f: File | undefined) {
    setError(null);
    if (!f) return setArchivo(null);
    const invalido = pdfInvalido(f);
    if (invalido) return setError(invalido);
    setArchivo(f);
  }

  async function registrar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!archivo) return setError("Selecciona el comprobante en PDF.");
    const datos = new FormData();
    datos.set("fecha", new FormData(e.currentTarget).get("fecha") as string);
    datos.set("archivo", archivo);
    setEnviando(true);
    const fallo = await enviarFormulario("/api/toefl", datos);
    setEnviando(false);
    if (fallo) setError(fallo);
    else onListo();
  }

  return (
    <form onSubmit={registrar} className="flex flex-col gap-[18px]">
      <div className="w-[280px]">
        <CampoPortal etiqueta="Fecha del examen" name="fecha" type="date" required max={hoy()} />
      </div>
      <div className="flex flex-col gap-1.5">
        <span id="comprobante" className="text-sm font-semibold text-portal-menu-texto">Comprobante (PDF)</span>
        <ZonaCarga archivo={archivo} onElegir={elegir} etiquetadoPor="comprobante" />
      </div>
      {error && <Alerta tipo="error">{error}</Alerta>}
      <div className="flex justify-end">
        <BotonPortal type="submit" disabled={enviando}>Registrar examen</BotonPortal>
      </div>
    </form>
  );
}
