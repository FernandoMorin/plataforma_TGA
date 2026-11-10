"use client";

import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, CampoPortal, Dato, Insignia, SelectorPortal, type Tono } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { sesionVencida } from "@/lib/acceso";
import { pagina } from "@/lib/menu";
import { tonoTitulacion } from "../alumnos/consulta";
import { fecha } from "../alumnos/[clave]/pestanas";

// Mi trabajo del alumno (Figma AT1 solicitud, AT2 enviada, AT7 rechazada, AT3 trabajo aceptado).

type Solicitud = {
  estado: "Pendiente" | "Rechazada";
  modalidad: string | null;
  titulo: string | null;
  linea: string | null;
  director: string | null;
  idModalidad: number | null;
  idLineaInvestigacion: number | null;
  fechaSolicitud: string | null;
  fechaResolucion: string | null;
  motivoRechazo: string | null;
};
type Trabajo = {
  modalidad: string | null;
  estado: string;
  titulo: string | null;
  director: string | null;
  linea: string | null;
  fechaRegistro: string | null;
  avance: number;
  ultimoInforme: { numero: number; fecha: string | null } | null;
  siguiente: { numero: number; fin: string | null } | null;
};
type Opciones = {
  modalidades: { id: number; nombre: string }[];
  lineas: { id: number; nombre: string }[];
  directores: { rpe: string; nombre: string }[];
};

const tonoSolicitud: Record<Solicitud["estado"], Tono> = { Pendiente: "ambar", Rechazada: "rojo" };
const meses = ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];
const mesAnio = (iso: string) => `${meses[Number(iso.slice(5, 7)) - 1]} ${iso.slice(0, 4)}`;

export function MiTrabajo() {
  const [datos, setDatos] = useState<{ solicitud: Solicitud | null; trabajo: Trabajo | null } | null>(null);
  const [version, setVersion] = useState(0);
  // Tras un rechazo, «Elegir otro director» vuelve a mostrar el formulario con los datos anteriores.
  const [reintentar, setReintentar] = useState(false);

  useEffect(() => {
    fetch("/api/mi-titulacion").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
    });
  }, [version]);

  const p = pagina("/mi-titulacion")!;
  if (!datos) return <EncabezadoPagina titulo={p.titulo} />;
  if (datos.trabajo) return <TrabajoAceptado trabajo={datos.trabajo} />;

  const s = datos.solicitud;
  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion="Elige tu modalidad y a tu director." />
      <Tarjeta titulo="Solicitud de dirección" insignia={s && !(s.estado === "Rechazada" && reintentar) && <Insignia tono={tonoSolicitud[s.estado]}>{s.estado}</Insignia>}>
        {!s || reintentar ? (
          <FormSolicitud anterior={s} onListo={() => { setReintentar(false); setVersion((v) => v + 1); }} />
        ) : s.estado === "Pendiente" ? (
          <>
            <dl className="flex flex-col gap-2.5">
              <Dato etiqueta="Modalidad">{s.modalidad}</Dato>
              <Dato etiqueta="Título">{s.titulo}</Dato>
              <Dato etiqueta="Línea de investigación">{s.linea}</Dato>
              <Dato etiqueta="Director">{s.director}</Dato>
              <Dato etiqueta="Enviada">{fecha(s.fechaSolicitud)}</Dato>
            </dl>
            <Alerta tipo="info">Esperando respuesta de {s.director}.</Alerta>
          </>
        ) : (
          <>
            <dl className="flex flex-col gap-2.5">
              <Dato etiqueta="Director">{s.director}</Dato>
              <Dato etiqueta="Título">{s.titulo}</Dato>
              <Dato etiqueta="Respondida">{fecha(s.fechaResolucion)}</Dato>
            </dl>
            <Alerta tipo="advertencia">Motivo: {s.motivoRechazo}</Alerta>
            <div className="flex justify-end">
              <BotonPortal onClick={() => setReintentar(true)}>Elegir otro director</BotonPortal>
            </div>
          </>
        )}
      </Tarjeta>
    </>
  );
}

function Tarjeta({ titulo, insignia, children }: { titulo: string; insignia?: ReactNode; children: ReactNode }) {
  return (
    <section className="mx-auto flex w-[720px] flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
      <div className="flex items-center justify-between">
        <h2 className="text-[17px] font-semibold text-portal-titulo">{titulo}</h2>
        {insignia}
      </div>
      {children}
    </section>
  );
}

function FormSolicitud({ anterior, onListo }: { anterior: Solicitud | null; onListo: () => void }) {
  const [opciones, setOpciones] = useState<Opciones | null>(null);
  const [modalidad, setModalidad] = useState<number | null>(anterior?.idModalidad ?? null);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch("/api/mi-titulacion/opciones").then(async (res) => res.ok && setOpciones(await res.json()));
  }, []);

  async function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!modalidad) return setError("Elige una modalidad.");
    const f = Object.fromEntries(new FormData(e.currentTarget)) as Record<string, string>;
    setEnviando(true);
    const fallo = await enviarJson("/api/mi-titulacion/solicitud", "POST", {
      idModalidad: modalidad, titulo: f.titulo, idLineaInvestigacion: Number(f.linea), rpeDirector: f.director,
    });
    setEnviando(false);
    if (fallo) setError(fallo);
    else onListo();
  }

  if (!opciones) return null;
  return (
    <form onSubmit={enviar} className="flex flex-col gap-4">
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-semibold text-portal-menu-texto">Modalidad de titulación</legend>
        <div className="flex gap-3">
          {opciones.modalidades.map((m) => {
            const elegida = m.id === modalidad;
            return (
              <label key={m.id} className={`flex flex-1 cursor-pointer items-center gap-3 rounded-[10px] px-4 py-3.5 has-focus-visible:outline-2 has-focus-visible:outline-portal-activo ${elegida ? "border-[1.5px] border-portal-activo bg-portal-activo-fondo" : "border border-portal-borde-campo bg-blanco"}`}>
                <input type="radio" name="modalidad" value={m.id} checked={elegida} onChange={() => setModalidad(m.id)} className="sr-only" />
                <span className={`flex size-[18px] items-center justify-center rounded-full border-[1.5px] ${elegida ? "border-portal-activo" : "border-portal-borde-campo"}`} aria-hidden>
                  {elegida && <span className="size-2 rounded-full bg-portal-activo" />}
                </span>
                <span className="text-sm font-semibold text-portal-titulo">{m.nombre}</span>
              </label>
            );
          })}
        </div>
      </fieldset>
      <CampoPortal etiqueta="Título del trabajo" name="titulo" required maxLength={255} defaultValue={anterior?.titulo ?? ""} />
      <SelectorPortal etiqueta="Línea de investigación" name="linea" required
        defaultValue={anterior?.idLineaInvestigacion ?? (opciones.lineas.length === 1 ? opciones.lineas[0].id : "")}>
        <option value="" disabled>Seleccionar</option>
        {opciones.lineas.map((l) => <option key={l.id} value={l.id}>{l.nombre}</option>)}
      </SelectorPortal>
      <SelectorPortal etiqueta="Director" name="director" required defaultValue="">
        <option value="" disabled>Seleccionar</option>
        {opciones.directores.map((d) => <option key={d.rpe} value={d.rpe}>{d.nombre}</option>)}
      </SelectorPortal>
      {error && <Alerta tipo="error">{error}</Alerta>}
      <div className="flex justify-end">
        <BotonPortal type="submit" disabled={enviando}>Enviar solicitud</BotonPortal>
      </div>
    </form>
  );
}

function TrabajoAceptado({ trabajo: t }: { trabajo: Trabajo }) {
  const avance = Math.round(t.avance);
  return (
    <>
      <EncabezadoPagina titulo="Mi trabajo" />
      <div className="flex items-start gap-5">
        <section className="flex flex-1 flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <div className="flex items-center justify-between">
            <h2 className="text-[17px] font-semibold text-portal-titulo">{t.modalidad}</h2>
            <Insignia tono={tonoTitulacion[t.estado] ?? "gris"}>{t.estado}</Insignia>
          </div>
          <p className="text-lg font-semibold text-portal-titulo">{t.titulo}</p>
          <dl className="flex flex-col gap-2.5">
            <Dato etiqueta="Director">{t.director}</Dato>
            <Dato etiqueta="Línea de investigación">{t.linea}</Dato>
            <Dato etiqueta="Fecha de registro">{fecha(t.fechaRegistro)}</Dato>
          </dl>
        </section>
        <section className="flex flex-1 flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <h2 className="text-[17px] font-semibold text-portal-titulo">Avance</h2>
          <p className="text-4xl font-bold text-portal-activo">{avance} %</p>
          <div className="h-2 overflow-hidden rounded-[4px] bg-portal-neutro-fondo" role="progressbar" aria-label="Avance" aria-valuenow={avance} aria-valuemin={0} aria-valuemax={100}>
            <div className="h-full bg-portal-activo" style={{ width: `${Math.min(avance, 100)}%` }} />
          </div>
          <dl className="flex flex-col gap-2.5 text-sm">
            <div className="flex items-center gap-3">
              <dt className="w-[130px] shrink-0 text-portal-subtexto">Último informe</dt>
              <dd className="font-medium text-portal-titulo">{t.ultimoInforme ? `Informe ${t.ultimoInforme.numero} · ${fecha(t.ultimoInforme.fecha)}` : "—"}</dd>
            </div>
            <div className="flex items-center gap-3">
              <dt className="w-[130px] shrink-0 text-portal-subtexto">Siguiente</dt>
              <dd className="font-medium text-portal-titulo">{t.siguiente ? `Informe ${t.siguiente.numero}${t.siguiente.fin ? ` · ${mesAnio(t.siguiente.fin)}` : ""}` : "—"}</dd>
            </div>
          </dl>
        </section>
      </div>
    </>
  );
}
