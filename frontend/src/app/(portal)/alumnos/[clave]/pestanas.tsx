"use client";

import { sesionVencida } from "@/lib/acceso";

import { useEffect, useState, type ReactNode } from "react";
import { Download } from "lucide-react";
import { Dato, Insignia, TarjetaPortal, type Tono } from "@/components/modulo";
import { tonoTitulacion } from "../consulta";

// Pestañas Titulación (F2), Evaluaciones (F3) y Documentos (F4) de la ficha del alumno.

export const fecha = (iso: string | null) => (iso ? iso.slice(0, 10).split("-").reverse().join("/") : "—");
export const ordinal = (n: number) => `${n}.${n === 1 || n === 3 ? "er" : "º"}`;
const nombreCompuesto = (n: string) => n.split(" ").slice(0, 2).map((p) => p[0]).join("");

// Pide los datos de la pestaña al abrirla.
function useDatos<T>(ruta: string) {
  const [datos, setDatos] = useState<T | null>(null);
  useEffect(() => {
    fetch(ruta).then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
    });
  }, [ruta]);
  return datos;
}

type Titulacion = {
  trabajo: { titulo: string | null; estado: string; modalidad: string | null; fechaRegistro: string | null; avance: number | null } | null;
  comite: { clave: string; integrantes: { nombre: string; rol: string }[] } | null;
  informes: { numero: number; fecha: string | null; avance: number | null; cerrado: boolean }[];
};

export function PestanaTitulacion({ clave, semestre, esperado }: { clave: string; semestre: number | null; esperado: number | null }) {
  const d = useDatos<Titulacion>(`/api/alumnos/${encodeURIComponent(clave)}/titulacion`);
  if (!d) return null;
  const t = d.trabajo;

  return (
    <>
      <div className="grid grid-cols-2 items-start gap-5">
        <TarjetaPortal titulo={t?.modalidad ?? "Trabajo de titulación"} insignia={t && <Insignia tono={tonoTitulacion[t.estado] ?? "gris"}>{t.estado}</Insignia>}>
          {t ? (
            <>
              <p className="text-lg font-semibold text-portal-titulo">{t.titulo}</p>
              <dl className="flex flex-col gap-2.5">
                <Dato etiqueta="Fecha de registro">{fecha(t.fechaRegistro)}</Dato>
                <Dato etiqueta="Avance">{t.avance === null ? "—" : `${Math.round(t.avance)} %`}</Dato>
                <Dato etiqueta="Modalidad">{t.modalidad ?? "—"}</Dato>
              </dl>
            </>
          ) : (
            <p className="text-sm text-portal-placeholder">Sin trabajo registrado</p>
          )}
        </TarjetaPortal>

        <TarjetaPortal titulo="Comité tutorial" insignia={d.comite && <span className="text-[13px] font-medium text-portal-subtexto">{d.comite.clave}</span>}>
          {d.comite?.integrantes.length ? (
            <ul className="flex flex-col gap-3.5">
              {d.comite.integrantes.map((p) => (
                <li key={p.nombre} className="flex items-center gap-3">
                  <span className="flex size-9 items-center justify-center rounded-full bg-portal-neutro-fondo text-[13px] font-semibold text-portal-subtexto">{nombreCompuesto(p.nombre)}</span>
                  <span className="flex-1 text-sm font-medium text-portal-titulo">{p.nombre}</span>
                  <Insignia tono="gris">{p.rol}</Insignia>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-sm text-portal-placeholder">Sin comité asignado</p>
          )}
        </TarjetaPortal>
      </div>

      {t && t.avance !== null && (
        <TarjetaPortal titulo="Avance por informe">
          <GraficaAvance registro={t.fechaRegistro} informes={d.informes} esperado={esperado} />
          <div className="flex gap-6 text-[13px]">
            <span className="text-portal-activo">● Avance registrado</span>
            {semestre && esperado !== null && (
              <span className="text-portal-error-texto">- - Esperado en {ordinal(semestre)} semestre ({Math.round(esperado)} %)</span>
            )}
          </div>
        </TarjetaPortal>
      )}
    </>
  );
}

// Gráfica de línea del avance (Figma F2, «Avance por informe»): registro en 0 %, un punto por informe cerrado,
// el informe en curso como círculo punteado y el avance esperado como línea punteada.
function GraficaAvance({ registro, informes, esperado }: { registro: string | null; informes: Titulacion["informes"]; esperado: number | null }) {
  const ancho = 1060, izq = 50, arriba = 20, abajo = 170;
  const y = (v: number) => abajo - (v / 100) * (abajo - arriba);
  let ultimo = 0;
  const puntos = [
    { etiqueta: `Registro · ${fecha(registro)}`, avance: 0 as number | null, cerrado: true },
    ...informes.map((i) => ({
      etiqueta: `Informe ${i.numero} · ${i.cerrado ? fecha(i.fecha) : "en curso"}`,
      avance: i.cerrado ? i.avance ?? 0 : null,
      cerrado: i.cerrado,
    })),
  ].map((p, i, todos) => {
    if (p.avance !== null) ultimo = p.avance;
    // El informe en curso se dibuja a la altura del último avance registrado.
    return { ...p, x: izq + ((i + 0.5) * (ancho - izq)) / todos.length, yv: y(p.avance ?? ultimo) };
  });
  const linea = puntos.filter((p) => p.cerrado).map((p) => `${p.x},${p.yv}`).join(" ");

  return (
    <svg viewBox={`0 0 ${ancho} 215`} className="w-full" role="img" aria-label="Avance por informe">
      {[0, 25, 50, 75, 100].map((v) => (
        <g key={v}>
          <line x1={izq} x2={ancho} y1={y(v)} y2={y(v)} className="stroke-portal-borde" />
          <text x={0} y={y(v) + 4} className="fill-portal-placeholder text-xs">{v} %</text>
        </g>
      ))}
      {esperado !== null && <line x1={izq} x2={ancho} y1={y(esperado)} y2={y(esperado)} strokeDasharray="6 4" strokeWidth={1.5} className="stroke-portal-error-texto" />}
      <polyline points={linea} fill="none" strokeWidth={2} className="stroke-portal-activo" />
      {puntos.map((p) => (
        <g key={p.etiqueta}>
          {p.cerrado ? (
            <circle cx={p.x} cy={p.yv} r={5} className="fill-portal-activo" />
          ) : (
            <circle cx={p.x} cy={p.yv} r={5} fill="none" strokeWidth={1.5} strokeDasharray="2 2" className="stroke-portal-placeholder" />
          )}
          {/* Área de puntero más grande que el punto, con el valor como dato emergente. */}
          <circle cx={p.x} cy={p.yv} r={12} fill="transparent">
            <title>{p.avance === null ? `${p.etiqueta}` : `${p.etiqueta}: ${Math.round(p.avance)} %`}</title>
          </circle>
          <text x={p.x} y={200} textAnchor="middle" className={`text-xs ${p.cerrado ? "fill-portal-subtexto" : "fill-portal-placeholder"}`}>{p.etiqueta}</text>
        </g>
      ))}
    </svg>
  );
}

const tonoEvaluacion: Record<string, Tono> = { Abierta: "gris", "En consolidación": "ambar", "En firma": "azul", Cerrada: "verde" };

type Evaluacion = {
  numero: number; periodo: string | null; fecha: string | null; avance: number | null; promedio: number | null; estado: string;
  acta: { id: number; nombre: string | null } | null;
};

export function PestanaEvaluaciones({ clave }: { clave: string }) {
  const filas = useDatos<Evaluacion[]>(`/api/alumnos/${encodeURIComponent(clave)}/evaluaciones`);
  if (!filas) return null;
  return (
    <Tabla columnas={["Informe", "Periodo", "Fecha", "Avance", "Promedio", "Estado", "Acta"]} vacia="Sin evaluaciones registradas">
      {filas.map((e) => (
        <tr key={e.numero} className="border-t border-portal-borde">
          <td className="py-4 pl-5 font-semibold text-portal-titulo">Informe {e.numero}</td>
          <td>{e.periodo ?? "—"}</td>
          <td>{fecha(e.fecha)}</td>
          <td className="font-semibold text-portal-titulo">{e.avance === null ? "—" : `${Math.round(e.avance)} %`}</td>
          <td className="font-semibold text-portal-titulo">{e.promedio ?? "—"}</td>
          <td><Insignia tono={tonoEvaluacion[e.estado] ?? "gris"}>{e.estado}</Insignia></td>
          <td>{e.acta ? <a href={`/api/documentos/${e.acta.id}`} className="font-semibold text-portal-activo">{e.acta.nombre}</a> : <span className="text-portal-placeholder">—</span>}</td>
        </tr>
      ))}
    </Tabla>
  );
}

type Documento = { id: number; nombre: string | null; tipo: string | null; asociado: string | null; fechaCarga: string | null };

export function PestanaDocumentos({ clave }: { clave: string }) {
  const filas = useDatos<Documento[]>(`/api/alumnos/${encodeURIComponent(clave)}/documentos`);
  if (!filas) return null;
  return (
    <Tabla columnas={["Documento", "Tipo", "Asociado a", "Fecha de carga", ""]} vacia="Sin documentos">
      {filas.map((d) => (
        <tr key={d.id} className="border-t border-portal-borde">
          <td className="py-5 pl-5 font-semibold text-portal-titulo">{d.nombre}</td>
          <td>{d.tipo && <Insignia tono="gris">{d.tipo}</Insignia>}</td>
          <td>{d.asociado ?? "—"}</td>
          <td>{fecha(d.fechaCarga)}</td>
          <td className="pr-5">
            <a href={`/api/documentos/${d.id}`} className="inline-flex items-center gap-1.5 font-semibold text-portal-activo">
              <Download size={16} aria-hidden />
              Descargar
            </a>
          </td>
        </tr>
      ))}
    </Tabla>
  );
}

function Tabla({ columnas, vacia, children }: { columnas: string[]; vacia: string; children: ReactNode[] }) {
  return (
    <div className="overflow-hidden rounded-xl border border-portal-borde">
      <table className="w-full text-left text-sm text-portal-menu-texto">
        <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
          <tr>
            {columnas.map((c, i) => (
              <th key={i} className={`py-2.5 font-semibold ${i === 0 ? "pl-5" : ""}`}>{c || <span className="sr-only">Acciones</span>}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {children.length ? children : (
            <tr className="border-t border-portal-borde">
              <td colSpan={columnas.length} className="py-4 pl-5 text-portal-placeholder">{vacia}</td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
