"use client";

import { useState, type FormEvent, type ReactNode } from "react";
import { BotonPortal, CampoPortal, Dialogo, Insignia, SelectorPortal } from "@/components/modulo";
import { tonoEstado } from "../consulta";

// Diálogos de la ficha: Editar datos (F5), Cambiar estado (F6), Aprobar TOEFL (F7) y No aprobado.

export type Opciones = {
  generaciones: { id: number; nombre: string | null }[];
  periodos: { id: number; nombre: string | null }[];
  modalidades: { id: number; nombre: string }[];
  estados: { id: number; nombre: string }[];
};

type Base = { abierto: boolean; onCerrar: () => void; onListo: () => void; subtitulo: string; clave: number };

// Envía el formulario al backend; si falla, deja el mensaje para mostrarlo en el diálogo.
function useEnvio(onListo: () => void) {
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  async function enviar(ruta: string, metodo: string, cuerpo: unknown) {
    setEnviando(true);
    setError(null);
    try {
      const res = await fetch(ruta, { method: metodo, headers: { "Content-Type": "application/json" }, body: JSON.stringify(cuerpo) });
      if (res.status === 401) return window.location.replace("/?sesion=terminada");
      if (res.ok) return onListo();
      const r = await res.json().catch(() => ({}));
      setError(r.mensaje ?? "Revisa los datos e intenta de nuevo.");
    } catch {
      setError("No se pudo guardar. Intenta de nuevo.");
    } finally {
      setEnviando(false);
    }
  }
  return { error, enviando, enviar };
}

function Acciones({ onCerrar, enviando, error, children }: { onCerrar: () => void; enviando: boolean; error: string | null; children: ReactNode }) {
  return (
    <>
      {error && <p role="alert" className="text-sm text-portal-error-texto">{error}</p>}
      <div className="flex justify-end gap-2.5">
        <BotonPortal type="button" variante="contorno" onClick={onCerrar}>Cancelar</BotonPortal>
        <BotonPortal type="submit" disabled={enviando}>{children}</BotonPortal>
      </div>
    </>
  );
}

const vacioANulo = (v: FormDataEntryValue | null) => (typeof v === "string" && v.trim() ? v.trim() : null);

export function DialogoEditarDatos({ datos, opciones, ...p }: Base & {
  opciones: Opciones;
  datos: { correoInstitucional: string | null; correoPersonal: string | null; telefono: string | null; idGeneracion: number | null; semestre: number | null; idPeriodo: number | null; idModalidad: number | null };
}) {
  const { error, enviando, enviar } = useEnvio(p.onListo);
  function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    enviar(`/api/alumnos/${p.clave}/datos`, "PUT", {
      correoInstitucional: f.get("correoInstitucional"),
      correoPersonal: vacioANulo(f.get("correoPersonal")),
      telefono: vacioANulo(f.get("telefono")),
      idGeneracion: Number(f.get("idGeneracion")),
      semestre: Number(f.get("semestre")),
      idPeriodo: Number(f.get("idPeriodo")),
      idModalidad: f.get("idModalidad") ? Number(f.get("idModalidad")) : null,
    });
  }
  return (
    <Dialogo abierto={p.abierto} onCerrar={p.onCerrar} titulo="Editar datos" subtitulo={p.subtitulo} ancho="w-[640px]">
      <form onSubmit={guardar} className="flex flex-col gap-5">
        <div className="grid grid-cols-2 gap-x-5 gap-y-4">
          <CampoPortal etiqueta="Correo institucional" name="correoInstitucional" type="email" required maxLength={100} defaultValue={datos.correoInstitucional ?? ""} />
          <CampoPortal etiqueta="Correo personal" name="correoPersonal" type="email" maxLength={100} defaultValue={datos.correoPersonal ?? ""} />
          <CampoPortal etiqueta="Teléfono" name="telefono" type="tel" maxLength={20} defaultValue={datos.telefono ?? ""} />
          <SelectorPortal etiqueta="Generación" name="idGeneracion" required defaultValue={datos.idGeneracion ?? ""}>
            <option value="" disabled>Seleccionar</option>
            {opciones.generaciones.map((g) => <option key={g.id} value={g.id}>{g.nombre}</option>)}
          </SelectorPortal>
          <SelectorPortal etiqueta="Semestre" name="semestre" required defaultValue={datos.semestre ?? ""}>
            <option value="" disabled>Seleccionar</option>
            {[1, 2, 3, 4, 5, 6, 7, 8].map((n) => <option key={n} value={n}>{n}</option>)}
          </SelectorPortal>
          <SelectorPortal etiqueta="Periodo" name="idPeriodo" required defaultValue={datos.idPeriodo ?? ""}>
            <option value="" disabled>Seleccionar</option>
            {opciones.periodos.map((x) => <option key={x.id} value={x.id}>{x.nombre}</option>)}
          </SelectorPortal>
          <SelectorPortal etiqueta="Modalidad de titulación" name="idModalidad" defaultValue={datos.idModalidad ?? ""}>
            <option value="">Sin asignar</option>
            {opciones.modalidades.map((m) => <option key={m.id} value={m.id}>{m.nombre}</option>)}
          </SelectorPortal>
        </div>
        <Acciones onCerrar={p.onCerrar} enviando={enviando} error={error}>Guardar cambios</Acciones>
      </form>
    </Dialogo>
  );
}

// Textos de Figma F6 para Baja temporal, Baja definitiva y Titulado.
const descripcionEstado: Record<string, string> = {
  Activo: "Reingresa al programa.",
  "Baja temporal": "Puede reingresar después.",
  "Baja definitiva": "Deja el programa.",
  Titulado: "Concluyó su titulación.",
};

export function DialogoCambiarEstado({ actual, transiciones, opciones, ...p }: Base & {
  actual: string;
  transiciones: { id: number; disponible: boolean }[];
  opciones: Opciones;
}) {
  const { error, enviando, enviar } = useEnvio(p.onListo);
  const [elegido, setElegido] = useState<number | null>(null);
  const nombre = (id: number) => opciones.estados.find((e) => e.id === id)?.nombre ?? "";

  return (
    <Dialogo abierto={p.abierto} onCerrar={p.onCerrar} titulo="Cambiar estado académico" subtitulo={p.subtitulo} ancho="w-[520px]">
      <form
        onSubmit={(e) => { e.preventDefault(); if (elegido) enviar(`/api/alumnos/${p.clave}/estado`, "POST", { idEstado: elegido }); }}
        className="flex flex-col gap-5"
      >
        <div className="flex items-center gap-2.5 text-sm text-portal-subtexto">
          Estado actual <Insignia tono={tonoEstado[actual] ?? "gris"}>{actual}</Insignia>
        </div>
        <fieldset className="flex flex-col gap-2.5">
          <legend className="sr-only">Nuevo estado</legend>
          {transiciones.map((t) => {
            const marcado = elegido === t.id;
            return (
              <label
                key={t.id}
                className={`flex items-center gap-3 rounded-[10px] px-4 py-3.5 ${t.disponible ? "cursor-pointer" : "cursor-not-allowed opacity-60"} ${marcado ? "border-[1.5px] border-portal-activo bg-portal-activo-fondo" : "border border-portal-borde-campo bg-blanco"}`}
              >
                <input type="radio" name="estado" className="sr-only" disabled={!t.disponible} checked={marcado} onChange={() => setElegido(t.id)} />
                <span className={`flex size-[18px] shrink-0 items-center justify-center rounded-full border-[1.5px] ${marcado ? "border-portal-activo" : "border-portal-borde-campo"}`} aria-hidden>
                  {marcado && <span className="size-2.5 rounded-full bg-portal-activo" />}
                </span>
                <span className="flex flex-col gap-0.5">
                  <span className="text-sm font-semibold text-portal-titulo">{nombre(t.id)}</span>
                  <span className="text-[13px] text-portal-subtexto">
                    {t.disponible ? descripcionEstado[nombre(t.id)] : "Requiere el trabajo de titulación aceptado."}
                  </span>
                </span>
              </label>
            );
          })}
        </fieldset>
        <Acciones onCerrar={p.onCerrar} enviando={enviando || !elegido} error={error}>Cambiar estado</Acciones>
      </form>
    </Dialogo>
  );
}

export function DialogoToefl({ modo, comprobante, ...p }: Base & { modo: "aprobar" | "rechazar"; comprobante: { id: number; nombre: string | null } | null }) {
  const { error, enviando, enviar } = useEnvio(p.onListo);
  const aprobar = modo === "aprobar";
  function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    if (aprobar) enviar(`/api/alumnos/${p.clave}/toefl/aprobar`, "POST", { puntaje: Number(f.get("puntaje")) });
    else enviar(`/api/alumnos/${p.clave}/toefl/rechazar`, "POST", { observaciones: f.get("observaciones") });
  }
  return (
    <Dialogo abierto={p.abierto} onCerrar={p.onCerrar} titulo={aprobar ? "Aprobar requisito TOEFL" : "No aprobar requisito TOEFL"} subtitulo={p.subtitulo} ancho="w-[460px]">
      <form onSubmit={guardar} className="flex flex-col gap-[18px]">
        <div className="flex gap-3 text-sm">
          <span className="w-[110px] text-portal-subtexto">Comprobante</span>
          {comprobante ? <a href={`/api/documentos/${comprobante.id}`} className="font-semibold text-portal-activo">{comprobante.nombre}</a> : "—"}
        </div>
        {aprobar ? (
          <CampoPortal etiqueta="Puntaje" name="puntaje" type="number" required min={0.01} max={999.99} step={0.01} />
        ) : (
          <label className="flex flex-col gap-1.5">
            <span className="text-sm font-semibold text-portal-menu-texto">Observaciones</span>
            <textarea name="observaciones" required maxLength={1000} rows={3}
              className="w-full rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5 text-[15px] text-portal-titulo outline-none focus:border-portal-activo" />
          </label>
        )}
        <Acciones onCerrar={p.onCerrar} enviando={enviando} error={error}>{aprobar ? "Aprobar" : "No aprobar"}</Acciones>
      </form>
    </Dialogo>
  );
}
