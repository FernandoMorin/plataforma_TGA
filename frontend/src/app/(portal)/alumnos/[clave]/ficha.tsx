"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { BotonPortal, Dato, Insignia, Pestanas, TarjetaPortal, type Tono } from "@/components/modulo";
import { tonoEstado, tonoToefl } from "../consulta";
import { fecha, ordinal, PestanaDocumentos, PestanaEvaluaciones, PestanaTitulacion } from "./pestanas";
import { DialogoCambiarEstado, DialogoEditarDatos, DialogoToefl, type Opciones } from "./dialogos";

// Ficha del alumno (Figma F1-F4): encabezado y pestaña Resumen aquí; las demás en pestanas.tsx.

type Ficha = {
  clave: number;
  nombre: string | null;
  apellidoPaterno: string | null;
  apellidoMaterno: string | null;
  correoInstitucional: string | null;
  correoPersonal: string | null;
  telefono: string | null;
  fechaIngreso: string | null;
  generacion: string | null;
  semestre: number | null;
  periodo: string | null;
  estadoAcademico: string;
  modalidad: string;
  toefl: { estado: string; fechaPresentacion: string | null; puntaje: number | null; observaciones: string | null; comprobante: { id: number; nombre: string | null } | null };
  titulacion: { estado: string; avance: number | null; esperado: number | null; rezago: string; ultimoInforme: string | null };
  evaluaciones: number;
  documentos: number;
  idGeneracion: number | null;
  idPeriodo: number | null;
  idModalidad: number | null;
  transiciones: { id: number; disponible: boolean }[];
};

const tonoRezago: Record<string, Tono> = { "En rezago": "rojo", "Al corriente": "verde", "No aplica": "gris" };

const pestanas = [
  { valor: "resumen", texto: "Resumen" },
  { valor: "titulacion", texto: "Titulación" },
  { valor: "evaluaciones", texto: "Evaluaciones" },
  { valor: "documentos", texto: "Documentos" },
] as const;
type Pestana = (typeof pestanas)[number]["valor"];

export function FichaAlumno() {
  const { clave } = useParams<{ clave: string }>();
  const [a, setA] = useState<Ficha | null>(null);
  const [noExiste, setNoExiste] = useState(false);
  const [pestana, setPestana] = useState<Pestana>("resumen");
  const [opciones, setOpciones] = useState<Opciones | null>(null);
  const [dialogo, setDialogo] = useState<"datos" | "estado" | "aprobar" | "rechazar" | null>(null);
  // Se incrementa al guardar para volver a pedir la ficha.
  const [version, setVersion] = useState(0);

  useEffect(() => {
    fetch(`/api/alumnos/${encodeURIComponent(clave)}`).then(async (res) => {
      if (res.status === 401) window.location.replace("/?sesion=terminada");
      else if (res.ok) setA(await res.json());
      else setNoExiste(true);
    });
  }, [clave, version]);

  useEffect(() => {
    fetch("/api/alumnos/opciones").then(async (res) => res.ok && setOpciones(await res.json()));
  }, []);

  const volver = (
    <Link href="/alumnos" className="text-sm font-semibold text-portal-activo">
      ← Consulta de alumnos
    </Link>
  );
  if (noExiste) return <>{volver}<p className="text-portal-subtexto">No existe un alumno con la clave {clave}.</p></>;
  if (!a) return volver;

  const nombre = [a.nombre, a.apellidoPaterno, a.apellidoMaterno].filter(Boolean).join(" ");
  const iniciales = `${a.nombre?.[0] ?? ""}${a.apellidoPaterno?.[0] ?? ""}`;
  const t = a.titulacion;

  return (
    <>
      {volver}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <span className="flex size-14 items-center justify-center rounded-full bg-portal-activo-fondo text-lg font-bold text-portal-activo">{iniciales}</span>
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center gap-2.5">
              <h1 className="text-2xl font-bold text-portal-titulo">{nombre}</h1>
              <Insignia tono={tonoEstado[a.estadoAcademico] ?? "gris"}>{a.estadoAcademico}</Insignia>
              {t.rezago === "En rezago" && <Insignia tono="rojo">En rezago</Insignia>}
            </div>
            <p className="text-[15px] text-portal-descripcion">
              {[a.clave, a.correoInstitucional, a.generacion && `Generación ${a.generacion}`, a.semestre && `${ordinal(a.semestre)} semestre`].filter(Boolean).join(" · ")}
            </p>
          </div>
        </div>
        <div className="flex gap-2.5">
          <BotonPortal variante="contorno" disabled={!opciones} onClick={() => setDialogo("datos")}>Editar datos</BotonPortal>
          {/* Titulado y Baja definitiva son estados finales: no hay a dónde cambiar. */}
          {a.transiciones.length > 0 && (
            <BotonPortal variante="contorno" disabled={!opciones} onClick={() => setDialogo("estado")}>Cambiar estado</BotonPortal>
          )}
        </div>
      </div>

      <Pestanas
        pestanas={pestanas.map((p) => ({ ...p, conteo: p.valor === "evaluaciones" ? a.evaluaciones : p.valor === "documentos" ? a.documentos : undefined }))}
        actual={pestana}
        onCambio={setPestana}
        espacio="gap-7"
      />

      {pestana === "resumen" ? (
        <div className="grid grid-cols-2 items-start gap-5">
          <TarjetaPortal titulo="Situación académica">
            <dl className="flex flex-col gap-2.5">
              <Dato etiqueta="Estado académico"><Insignia tono={tonoEstado[a.estadoAcademico] ?? "gris"}>{a.estadoAcademico}</Insignia></Dato>
              <Dato etiqueta="Semestre">{a.semestre ? `${a.semestre}.º` : "—"}</Dato>
              <Dato etiqueta="Periodo">{a.periodo ?? "—"}</Dato>
              <Dato etiqueta="Fecha de ingreso">{fecha(a.fechaIngreso)}</Dato>
              <Dato etiqueta="Modalidad">{a.modalidad}</Dato>
            </dl>
          </TarjetaPortal>

          <TarjetaPortal titulo="Contacto">
            <dl className="flex flex-col gap-2.5">
              <Dato etiqueta="Correo institucional">{a.correoInstitucional ?? "—"}</Dato>
              <Dato etiqueta="Correo personal">{a.correoPersonal ?? "—"}</Dato>
              <Dato etiqueta="Teléfono">{a.telefono ?? "—"}</Dato>
            </dl>
          </TarjetaPortal>

          <TarjetaPortal titulo="Requisito TOEFL" insignia={<Insignia tono={tonoToefl[a.toefl.estado] ?? "gris"}>{a.toefl.estado}</Insignia>}>
            <dl className="flex flex-col gap-2.5">
              <Dato etiqueta="Fecha del examen">{fecha(a.toefl.fechaPresentacion)}</Dato>
              <Dato etiqueta="Comprobante">
                {a.toefl.comprobante ? <a href={`/api/documentos/${a.toefl.comprobante.id}`} className="font-semibold text-portal-activo">{a.toefl.comprobante.nombre}</a> : "—"}
              </Dato>
              {a.toefl.puntaje !== null && <Dato etiqueta="Puntaje">{a.toefl.puntaje}</Dato>}
              <Dato etiqueta="Observaciones">{a.toefl.observaciones || "—"}</Dato>
            </dl>
            {a.toefl.estado === "Presentado" && (
              <div className="flex justify-end gap-2.5">
                <BotonPortal variante="contorno" onClick={() => setDialogo("rechazar")}>No aprobado</BotonPortal>
                <BotonPortal onClick={() => setDialogo("aprobar")}>Aprobar</BotonPortal>
              </div>
            )}
          </TarjetaPortal>

          <TarjetaPortal titulo="Avance de titulación" insignia={<Insignia tono={tonoRezago[t.rezago]}>{t.rezago}</Insignia>}>
            {t.avance === null ? (
              <p className="text-sm text-portal-placeholder">{t.estado}</p>
            ) : (
              <>
                <Avance etiqueta="Avance actual" porcentaje={t.avance} color="bg-portal-activo" />
                {a.semestre && t.esperado !== null && (
                  <Avance etiqueta={`Esperado en ${ordinal(a.semestre)} semestre`} porcentaje={t.esperado} color="bg-portal-placeholder" />
                )}
                {t.ultimoInforme && <p className="text-[13px] text-portal-placeholder">Último informe: {fecha(t.ultimoInforme)}</p>}
              </>
            )}
          </TarjetaPortal>
        </div>
      ) : pestana === "titulacion" ? (
        <PestanaTitulacion key={version} clave={clave} semestre={a.semestre} esperado={t.esperado} />
      ) : pestana === "evaluaciones" ? (
        <PestanaEvaluaciones key={version} clave={clave} />
      ) : (
        <PestanaDocumentos key={version} clave={clave} />
      )}
      {opciones && (() => {
        const base = { onCerrar: () => setDialogo(null), onListo: () => { setDialogo(null); setVersion((v) => v + 1); }, subtitulo: `${nombre} · ${a.clave}`, clave: a.clave };
        return (
          <>
            <DialogoEditarDatos {...base} abierto={dialogo === "datos"} opciones={opciones} datos={a} />
            <DialogoCambiarEstado {...base} abierto={dialogo === "estado"} opciones={opciones} actual={a.estadoAcademico} transiciones={a.transiciones} />
            <DialogoToefl {...base} abierto={dialogo === "aprobar" || dialogo === "rechazar"} modo={dialogo === "rechazar" ? "rechazar" : "aprobar"} comprobante={a.toefl.comprobante} />
          </>
        );
      })()}
    </>
  );
}

function Avance({ etiqueta, porcentaje, color }: { etiqueta: string; porcentaje: number; color: string }) {
  return (
    <div className="flex flex-col gap-2">
      <div className="flex justify-between text-sm">
        <span className="text-portal-subtexto">{etiqueta}</span>
        <span className="font-semibold text-portal-titulo">{Math.round(porcentaje)} %</span>
      </div>
      <div className="h-2 overflow-hidden rounded bg-portal-neutro-fondo">
        <div className={`h-full ${color}`} style={{ width: `${Math.min(porcentaje, 100)}%` }} />
      </div>
    </div>
  );
}
