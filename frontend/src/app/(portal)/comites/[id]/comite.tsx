"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState, type FormEvent } from "react";
import { Alerta, BotonPortal, CampoPortal, Dialogo, Insignia, SelectorPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { sesionVencida } from "@/lib/acceso";
import { fecha } from "../../alumnos/[clave]/pestanas";
import { iniciales } from "../comites";

// Detalle del comité (Figma CM2) con sus cambios: reemplazar (CM3), agregar, cambiar rol y quitar.

type Integrante = { id: number; rpe: string; nombre: string; rol: string; idRol: number; desde: string | null };
type Datos = {
  clave: string;
  claveAlumno: number;
  alumno: string;
  modalidad: string | null;
  titulo: string | null;
  estado: string;
  editable: boolean;
  integrantes: Integrante[];
  historial: { fecha: string | null; accion: string; integrante: string; rolAnterior: string | null; rolNuevo: string | null }[];
  profesores: { rpe: string; nombre: string; externo: boolean }[];
};
type Cambio = { tipo: "reemplazar" | "agregar" | "rol" | "quitar"; integrante?: Integrante };

const roles = [{ id: 1, nombre: "Director" }, { id: 2, nombre: "Codirector" }, { id: 3, nombre: "Sinodal" }];
const tonoAccion: Record<string, "verde" | "azul" | "ambar" | "rojo"> = { Alta: "verde", Reemplazo: "azul", "Cambio de rol": "ambar", Baja: "rojo" };
const enlace = "text-[13px] font-semibold";

export function DetalleComite() {
  const router = useRouter();
  const id = useParams<{ id: string }>().id;
  const [datos, setDatos] = useState<Datos | null>(null);
  const [version, setVersion] = useState(0);
  const [cambio, setCambio] = useState<Cambio | null>(null);

  useEffect(() => {
    fetch(`/api/comites/${id}`).then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
      else router.replace("/comites");
    });
  }, [id, router, version]);

  const volver = <Link href="/comites" className="text-sm font-semibold text-portal-activo">← Comités tutoriales</Link>;
  if (!datos) return volver;
  const completo = datos.integrantes.length >= 4;

  return (
    <>
      {volver}
      <div className="flex items-center justify-between gap-6">
        <div className="flex min-w-0 flex-col gap-1.5">
          <div className="flex items-center gap-2.5">
            <h1 className="text-[28px] font-bold text-portal-titulo">{datos.clave}</h1>
            <Insignia tono={datos.estado === "Completo" ? "verde" : datos.estado === "Concluido" ? "gris" : "ambar"}>{datos.estado}</Insignia>
          </div>
          <p className="truncate text-[15px] text-portal-descripcion">{datos.alumno} · {datos.modalidad} · {datos.titulo}</p>
        </div>
        <BotonPortal variante="contorno" className="shrink-0 whitespace-nowrap" disabled={!datos.editable || completo} onClick={() => setCambio({ tipo: "agregar" })}>Agregar integrante</BotonPortal>
      </div>

      <ul className="flex gap-4">
        {datos.integrantes.map((m) => {
          const director = m.rol === "Director";
          return (
            <li key={m.id} className={`flex min-w-0 flex-1 flex-col items-center gap-2.5 rounded-xl border px-[18px] pt-[18px] pb-4 ${director ? "border-portal-info-borde bg-portal-activo-fondo" : "border-portal-borde bg-blanco"}`}>
              <span aria-hidden className={`flex size-[52px] items-center justify-center rounded-full text-[17px] font-bold ${director ? "bg-portal-activo text-blanco" : "bg-portal-neutro-fondo text-portal-subtexto"}`}>
                {iniciales(m.nombre)}
              </span>
              <p className="text-center text-sm font-semibold text-portal-titulo">{m.nombre}</p>
              <Insignia tono={director ? "azul" : "gris"}>{m.rol}</Insignia>
              <p className="text-[13px] text-portal-subtexto">Desde {fecha(m.desde)}</p>
              {datos.editable && (
                <div className="flex flex-wrap justify-center gap-x-3 gap-y-1 pt-1.5">
                  <button onClick={() => setCambio({ tipo: "reemplazar", integrante: m })} className={`${enlace} text-portal-activo`}>Reemplazar</button>
                  {!director && <button onClick={() => setCambio({ tipo: "rol", integrante: m })} className={`${enlace} text-portal-activo`}>Cambiar rol</button>}
                  {!director && <button onClick={() => setCambio({ tipo: "quitar", integrante: m })} className={`${enlace} text-portal-error-texto`}>Quitar</button>}
                </div>
              )}
            </li>
          );
        })}
      </ul>

      <section className="overflow-hidden rounded-xl border border-portal-borde bg-blanco">
        <h2 className="px-5 pt-[18px] pb-3.5 text-[17px] font-semibold text-portal-titulo">Historial de cambios</h2>
        <table className="w-full table-fixed text-left">
          <colgroup><col className="w-[150px]" /><col className="w-[170px]" /><col className="w-[320px]" /><col className="w-[170px]" /><col /></colgroup>
          <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
            <tr>
              <th className="py-2.5 pl-5 font-semibold">Fecha</th>
              <th className="font-semibold">Acción</th>
              <th className="font-semibold">Integrante</th>
              <th className="font-semibold">Rol anterior</th>
              <th className="font-semibold">Rol nuevo</th>
            </tr>
          </thead>
          <tbody className="text-sm text-portal-menu-texto">
            {datos.historial.map((h, i) => (
              <tr key={i} className="border-t border-portal-borde">
                <td className="py-3 pl-5">{fecha(h.fecha)}</td>
                <td><Insignia tono={tonoAccion[h.accion] ?? "gris"}>{h.accion}</Insignia></td>
                <td className="font-medium text-portal-titulo">{h.integrante}</td>
                <td>{h.rolAnterior ?? <span className="text-portal-placeholder">—</span>}</td>
                <td>{h.rolNuevo ?? <span className="text-portal-placeholder">—</span>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <DialogoCambio id={id} datos={datos} cambio={cambio} onCerrar={() => setCambio(null)} onListo={() => { setCambio(null); setVersion((v) => v + 1); }} />
    </>
  );
}

const titulos = { reemplazar: "Reemplazar integrante", agregar: "Agregar integrante", rol: "Cambiar rol", quitar: "Quitar integrante" };
const botones = { reemplazar: "Reemplazar", agregar: "Agregar", rol: "Cambiar rol", quitar: "Quitar" };

function DialogoCambio({ id, datos, cambio, onCerrar, onListo }: { id: string; datos: Datos; cambio: Cambio | null; onCerrar: () => void; onListo: () => void }) {
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const tipo = cambio?.tipo ?? "agregar";
  const m = cambio?.integrante;
  const director = m?.rol === "Director";
  // Solo puede entrar quien no está ya; al director lo reemplaza un profesor de planta.
  const enComite = new Set(datos.integrantes.map((i) => i.rpe));
  const candidatos = datos.profesores.filter((p) => !enComite.has(p.rpe) && !(director && p.externo));
  const opcionesRol = director ? roles.filter((r) => r.id === 1) : roles.filter((r) => r.id !== 1 && !(tipo === "rol" && r.id === m?.idRol));

  async function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setEnviando(true);
    const fallo = await enviarJson(`/api/comites/${id}/${tipo}`, "POST", {
      idComiteProfesor: m?.id ?? null, rpe: f.get("rpe") || null, rol: f.get("rol") ? Number(f.get("rol")) : null, fecha: f.get("fecha"),
    });
    setEnviando(false);
    if (fallo) setError(fallo);
    else { setError(null); onListo(); }
  }

  const hoy = new Date().toLocaleDateString("en-CA");
  return (
    <Dialogo abierto={cambio !== null} onCerrar={() => { setError(null); onCerrar(); }} titulo={titulos[tipo]} subtitulo={`${datos.clave} · ${datos.alumno}`} ancho="w-[520px]">
      <form onSubmit={enviar} className="flex flex-col gap-4">
        {m && (
          <div className="flex items-center gap-3 rounded-[10px] bg-portal-tabla-cabecera px-3.5 py-3">
            <div className="flex flex-1 flex-col gap-0.5">
              <span className="text-xs text-portal-subtexto">{tipo === "rol" ? "Integrante" : "Sale"}</span>
              <span className="text-sm font-semibold text-portal-titulo">{m.nombre}</span>
            </div>
            <Insignia tono="gris">{m.rol}</Insignia>
          </div>
        )}
        {(tipo === "reemplazar" || tipo === "agregar") && (
          <SelectorPortal etiqueta="Entra" name="rpe" required defaultValue="">
            <option value="" disabled>Seleccionar profesor</option>
            {candidatos.map((p) => <option key={p.rpe} value={p.rpe}>{p.nombre}{p.externo ? " (externo)" : ""}</option>)}
          </SelectorPortal>
        )}
        {tipo !== "quitar" && (
          <SelectorPortal etiqueta="Rol" name="rol" required defaultValue={tipo === "reemplazar" ? m?.idRol : ""}>
            <option value="" disabled>Seleccionar rol</option>
            {opcionesRol.map((r) => <option key={r.id} value={r.id}>{r.nombre}</option>)}
          </SelectorPortal>
        )}
        <CampoPortal etiqueta="Fecha del cambio" name="fecha" type="date" required max={hoy} defaultValue={hoy} />
        {error && <Alerta tipo="error">{error}</Alerta>}
        <div className="flex justify-end gap-2.5">
          <BotonPortal type="button" variante="contorno" onClick={onCerrar}>Cancelar</BotonPortal>
          <BotonPortal type="submit" disabled={enviando} className={tipo === "quitar" ? "bg-portal-error-texto" : ""}>{botones[tipo]}</BotonPortal>
        </div>
      </form>
    </Dialogo>
  );
}
