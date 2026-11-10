"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { Alerta, AreaTexto, BotonPortal, CampoPortal, Casilla, Dato, Dialogo, Insignia, Pestanas } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { fechaCorta, ListaComentarios, TarjetaEmpleo, type Comentario, type Empleo } from "@/components/egresado";
import { sesionVencida } from "@/lib/acceso";
import { desdeUtc, fechaLocal } from "@/lib/fechas";
import { tonoSituacion } from "../egresados";

// Ficha del egresado (Figma E2 resumen, E3 intentos de contacto).

type Datos = {
  clave: number;
  identificador: string | null;
  generacion: string | null;
  egreso: string | null;
  nombre: string;
  estado: "Localizable" | "No localizable";
  activo: boolean;
  contacto: { correo: string | null; telefono: string | null; ciudad: string | null; otros: string | null; actualizado: string | null };
  situacion: string;
  tiempo: number | null;
  rango: string | null;
  actual: { empresa: string | null; puesto: string | null; area: string | null; desde: string | null; relacion: boolean | null } | null;
  empleos: Empleo[];
  intentos: { fecha: string | null; observaciones: string | null }[];
  comentarios: Comentario[];
  constancias: { id: number; nombre: string | null; fecha: string | null; revisada: boolean }[];
  sugerirNoLocalizable: boolean;
};
type Pestana = "resumen" | "historial" | "intentos" | "comentarios";

const iniciales = (n: string) => n.split(" ").slice(0, 2).map((p) => p[0]).join("").toUpperCase();

export function FichaEgresado() {
  const router = useRouter();
  const clave = useParams<{ clave: string }>().clave;
  const [datos, setDatos] = useState<Datos | null>(null);
  const [version, setVersion] = useState(0);
  const [pestana, setPestana] = useState<Pestana>("resumen");
  const [ahoraNo, setAhoraNo] = useState(false);
  const [confirmandoBaja, setConfirmandoBaja] = useState(false);
  const recargar = () => setVersion((v) => v + 1);

  useEffect(() => {
    fetch(`/api/egresados/${clave}`).then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
      else router.replace("/egresados");
    });
  }, [clave, router, version]);

  async function accion(ruta: string) {
    await enviarJson(`/api/egresados/${clave}/${ruta}`, "POST", {});
    recargar();
  }

  const volver = <Link href="/egresados" className="text-sm font-semibold text-portal-activo">← Consulta de egresados</Link>;
  if (!datos) return volver;
  const d = datos;

  return (
    <>
      {volver}
      <div className="flex items-center justify-between gap-6">
        <div className="flex min-w-0 items-center gap-4">
          <span aria-hidden className="flex size-14 shrink-0 items-center justify-center rounded-full bg-portal-activo-fondo text-lg font-bold text-portal-activo">{iniciales(d.nombre)}</span>
          <div className="flex min-w-0 flex-col gap-1.5">
            <div className="flex items-center gap-2.5">
              <h1 className="text-2xl font-bold text-portal-titulo">{d.nombre}</h1>
              {d.activo ? <Insignia tono={d.estado === "Localizable" ? "verde" : "rojo"}>{d.estado}</Insignia> : <Insignia tono="gris">Dado de baja</Insignia>}
            </div>
            <p className="text-[15px] text-portal-descripcion">{d.identificador} · Generación {d.generacion} · Egresó el {fechaCorta(d.egreso)}</p>
          </div>
        </div>
        <div className="flex shrink-0 gap-2.5">
          {d.activo && <BotonPortal variante="contorno" onClick={() => setPestana("intentos")}>Registrar intento de contacto</BotonPortal>}
          {d.activo
            ? <BotonPortal variante="contorno" onClick={() => setConfirmandoBaja(true)}>Dar de baja</BotonPortal>
            : <BotonPortal variante="contorno" onClick={() => accion("reactivar")}>Reactivar</BotonPortal>}
        </div>
      </div>

      <Pestanas espacio="gap-7" actual={pestana} onCambio={setPestana} pestanas={[
        { valor: "resumen", texto: "Resumen" },
        { valor: "historial", texto: "Historial laboral", conteo: d.empleos.length },
        { valor: "intentos", texto: "Intentos de contacto", conteo: d.intentos.length },
        { valor: "comentarios", texto: "Comentarios", conteo: d.comentarios.length },
      ]} />

      {pestana === "resumen" && <Resumen d={d} onCambio={recargar} />}
      {pestana === "historial" && (d.empleos.length === 0
        ? <p className="text-[15px] text-portal-subtexto">Sin empleos registrados.</p>
        : d.empleos.map((e) => <TarjetaEmpleo key={e.id} empleo={e} />))}
      {pestana === "intentos" && (
        <>
          {d.sugerirNoLocalizable && !ahoraNo && (
            <div className="flex items-center gap-3 rounded-[10px] border border-portal-advertencia-borde bg-portal-advertencia-fondo px-4 py-3.5">
              <p className="flex-1 text-sm font-semibold text-portal-advertencia-texto">{d.intentos.length} intentos sin respuesta. ¿Marcar como no localizable?</p>
              <BotonPortal variante="contorno" onClick={() => setAhoraNo(true)}>Ahora no</BotonPortal>
              <BotonPortal onClick={() => accion("no-localizable")}>Marcar no localizable</BotonPortal>
            </div>
          )}
          <div className="flex items-start gap-5">
            <section className="flex flex-1 flex-col rounded-xl border border-portal-borde bg-blanco px-[22px] pt-5 pb-2">
              <h2 className="font-semibold text-portal-titulo">Intentos de contacto</h2>
              {d.intentos.length === 0 && <p className="py-3.5 text-sm text-portal-subtexto">Sin intentos registrados.</p>}
              <ul>
                {d.intentos.map((i, n) => (
                  <li key={n} className="flex gap-4 border-b border-portal-borde py-3.5 text-sm last:border-b-0">
                    <span className="w-[100px] shrink-0 font-semibold text-portal-titulo">{fechaCorta(i.fecha)}</span>
                    <span className="text-portal-menu-texto">{i.observaciones}</span>
                  </li>
                ))}
              </ul>
            </section>
            {d.activo && <RegistrarIntento clave={d.clave} onListo={() => { setAhoraNo(false); recargar(); }} />}
          </div>
        </>
      )}
      {pestana === "comentarios" && (
        <section className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <h2 className="font-semibold text-portal-titulo">Comentarios</h2>
          {d.comentarios.length === 0 ? <p className="text-sm text-portal-subtexto">Sin comentarios.</p> : <ListaComentarios comentarios={d.comentarios} />}
        </section>
      )}

      <Dialogo abierto={confirmandoBaja} onCerrar={() => setConfirmandoBaja(false)} titulo="Dar de baja" subtitulo={`${d.nombre} · ${d.identificador}`} ancho="w-[460px]">
        <p className="text-sm text-portal-menu-texto">Su información se conserva y se puede reactivar.</p>
        <div className="flex justify-end gap-2.5">
          <BotonPortal variante="contorno" onClick={() => setConfirmandoBaja(false)}>Cancelar</BotonPortal>
          <BotonPortal onClick={async () => { setConfirmandoBaja(false); await accion("baja"); }}>Dar de baja</BotonPortal>
        </div>
      </Dialogo>
    </>
  );
}

function Tarjeta({ titulo, extra, children }: { titulo: string; extra?: ReactNode; children: ReactNode }) {
  return (
    <section className="flex flex-1 flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-[22px] py-5">
      <div className="flex items-center justify-between">
        <h2 className="font-semibold text-portal-titulo">{titulo}</h2>
        {extra}
      </div>
      {children}
    </section>
  );
}

function Resumen({ d, onCambio }: { d: Datos; onCambio: () => void }) {
  async function revisar(id: number, revisada: boolean) {
    await enviarJson(`/api/egresados/constancias/${id}`, "POST", { revisada });
    onCambio();
  }
  const indicadores: [string, string, string?][] = [
    ["Tiempo para emplearse", d.tiempo === null ? "—" : `${d.tiempo} ${d.tiempo === 1 ? "mes" : "meses"}`, d.rango ? `Rango: ${d.rango}` : undefined],
    ["Empleos registrados", String(d.empleos.length)],
    ["Intentos de contacto", String(d.intentos.length)],
  ];
  return (
    <>
      <dl className="flex gap-5">
        {indicadores.map(([t, v, nota]) => (
          <div key={t} className="flex flex-1 flex-col gap-1 rounded-xl bg-portal-tabla-cabecera px-[18px] py-4">
            <dt className="text-[13px] text-portal-subtexto">{t}</dt>
            <dd className="text-2xl font-bold text-portal-titulo">{v}</dd>
            {nota && <dd className="text-[13px] text-portal-subtexto">{nota}</dd>}
          </div>
        ))}
      </dl>
      <div className="flex items-start gap-5">
        <Tarjeta titulo="Contacto">
          <dl className="flex flex-col gap-2.5">
            <Dato etiqueta="Correo">{d.contacto.correo || "—"}</Dato>
            <Dato etiqueta="Teléfono">{d.contacto.telefono || "—"}</Dato>
            <Dato etiqueta="Ciudad">{d.contacto.ciudad || "—"}</Dato>
            <Dato etiqueta="Otros medios">{d.contacto.otros || "—"}</Dato>
            <Dato etiqueta="Última actualización">{fechaCorta(d.contacto.actualizado)}</Dato>
          </dl>
        </Tarjeta>
        <Tarjeta titulo="Empleo actual" extra={<Insignia tono={tonoSituacion[d.situacion] ?? "gris"}>{d.situacion}</Insignia>}>
          {d.actual ? (
            <dl className="flex flex-col gap-2.5">
              <Dato etiqueta="Empresa">{d.actual.empresa || "—"}</Dato>
              <Dato etiqueta="Puesto">{d.actual.puesto || "—"}</Dato>
              <Dato etiqueta="Área profesional">{d.actual.area || "—"}</Dato>
              <Dato etiqueta="Desde">{fechaCorta(d.actual.desde)}</Dato>
              <Dato etiqueta="Relación con la maestría">{d.actual.relacion === null ? "—" : d.actual.relacion ? "Sí" : "No"}</Dato>
            </dl>
          ) : <p className="text-sm text-portal-subtexto">Sin empleo vigente.</p>}
        </Tarjeta>
      </div>
      <section className="flex flex-col gap-1 rounded-xl border border-portal-borde bg-blanco px-[22px] pt-5 pb-3">
        <h2 className="font-semibold text-portal-titulo">Constancias</h2>
        {d.constancias.length === 0 && <p className="py-3 text-sm text-portal-subtexto">Sin constancias.</p>}
        <ul>
          {d.constancias.map((c) => (
            <li key={c.id} className="flex items-center gap-3 border-t border-portal-borde py-3">
              <a href={`/api/documentos/${c.id}`} className="flex-1 truncate text-sm font-semibold text-portal-activo">{c.nombre}</a>
              <span className="text-[13px] text-portal-subtexto">Subida el {c.fecha ? fechaLocal(desdeUtc(c.fecha)) : "—"}</span>
              <label className="flex cursor-pointer items-center gap-2 text-[13px] has-focus-visible:outline-2 has-focus-visible:outline-portal-activo">
                <input type="checkbox" className="sr-only" checked={c.revisada} onChange={() => revisar(c.id, !c.revisada)} />
                <Casilla marcada={c.revisada} />
                <span className={c.revisada ? "text-portal-exito-texto" : "text-portal-subtexto"}>Revisada</span>
              </label>
            </li>
          ))}
        </ul>
      </section>
    </>
  );
}

function RegistrarIntento({ clave, onListo }: { clave: number; onListo: () => void }) {
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const hoy = new Date().toLocaleDateString("en-CA");

  async function registrar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const form = e.currentTarget;
    const f = new FormData(form);
    setEnviando(true);
    const fallo = await enviarJson(`/api/egresados/${clave}/intentos`, "POST", { fecha: f.get("fecha"), observaciones: f.get("observaciones") });
    setEnviando(false);
    setError(fallo);
    if (!fallo) { form.reset(); onListo(); }
  }

  return (
    <form onSubmit={registrar} className="flex w-[380px] shrink-0 flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-[22px] py-5">
      <h2 className="font-semibold text-portal-titulo">Registrar intento</h2>
      <CampoPortal etiqueta="Fecha" name="fecha" type="date" required max={hoy} defaultValue={hoy} />
      <AreaTexto etiqueta="Observaciones" name="observaciones" required maxLength={1000} placeholder="Medio usado y resultado" />
      {error && <Alerta tipo="error">{error}</Alerta>}
      <div className="flex justify-end">
        <BotonPortal type="submit" disabled={enviando}>Registrar</BotonPortal>
      </div>
    </form>
  );
}
