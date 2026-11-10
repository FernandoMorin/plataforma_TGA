"use client";

import { sesionVencida } from "@/lib/acceso";

import Link from "next/link";
import { useEffect, useLayoutEffect, useRef, useState, type FormEvent } from "react";
import { ArrowLeft, ArrowRight, Copy, Download, Search, X } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, Casilla, CampoPortal, Insignia, Pasos, SelectorPortal, type Tono } from "@/components/modulo";
import { pagina } from "@/lib/menu";
import type { Opciones } from "../[clave]/dialogos";

// Importar alumnos desde el registro de la universidad (Figma I1-I3).

type Resultado = { clave: number; nombre: string; correo: string; estatus: string | null; situacion: "listo" | "baja" | "ya-esta" | "correo-en-uso" };
type Importado = { clave: number; nombre: string; correo: string; contrasena: string };
type Final = { importados: Importado[]; errores: { clave: number; mensaje: string }[] };

const situaciones: Record<Resultado["situacion"], { texto: string; tono: Tono }> = {
  listo: { texto: "Listo para importar", tono: "verde" },
  baja: { texto: "De baja en la universidad", tono: "rojo" },
  "ya-esta": { texto: "Ya está en el sistema", tono: "gris" },
  "correo-en-uso": { texto: "Su correo ya tiene cuenta", tono: "ambar" },
};

const esClave = (t: string) => /^\d{6}$/.test(t);

async function enviar<T>(ruta: string, cuerpo: unknown): Promise<{ datos?: T; error?: string }> {
  try {
    const res = await fetch(ruta, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(cuerpo) });
    if (res.status === 401) sesionVencida();
    if (res.ok) return { datos: await res.json() };
    return { error: (await res.json().catch(() => ({}))).mensaje ?? "No se pudo completar. Intenta de nuevo." };
  } catch {
    return { error: "No se pudo completar. Intenta de nuevo." };
  }
}

export function ImportarAlumnos() {
  const [paso, setPaso] = useState(0);
  const [claves, setClaves] = useState<string[]>([]);
  const [texto, setTexto] = useState("");
  const [busqueda, setBusqueda] = useState<{ resultados: Resultado[]; noEncontradas: number[] } | null>(null);
  const [seleccion, setSeleccion] = useState<number[]>([]);
  const [final, setFinal] = useState<Final | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [ocupado, setOcupado] = useState(false);
  const [opciones, setOpciones] = useState<Opciones | null>(null);

  useEffect(() => {
    fetch("/api/alumnos/opciones").then(async (res) => res.ok && setOpciones(await res.json()));
  }, []);

  // Las contraseñas solo se muestran esta vez: con cacheComponents la página se oculta en vez de
  // desmontarse, así que al salir se borra todo y al volver empieza de cero.
  const reiniciarAlSalir = useRef(false);
  reiniciarAlSalir.current = paso === 2;
  useLayoutEffect(() => () => {
    if (reiniciarAlSalir.current) reiniciar();
  }, []);

  function reiniciar() {
    setPaso(0); setClaves([]); setTexto(""); setBusqueda(null); setSeleccion([]); setFinal(null); setError(null);
  }

  // Las claves de 6 cifras se vuelven fichas; cualquier otro texto se queda como búsqueda por nombre.
  function agregar(entrada: string) {
    const partes = entrada.split(/[\s,;]+/).filter(Boolean);
    const nuevas = partes.filter(esClave);
    setClaves((c) => [...new Set([...c, ...nuevas])]);
    setTexto(partes.filter((p) => !esClave(p)).join(" "));
  }

  async function buscar() {
    const pendientes = texto.split(/[\s,;]+/).filter(esClave);
    const todas = [...new Set([...claves, ...pendientes])];
    const nombre = texto.split(/[\s,;]+/).filter((p) => p && !esClave(p)).join(" ") || null;
    setClaves(todas);
    setTexto(nombre ?? "");
    if (!todas.length && !nombre) return;
    setOcupado(true);
    setError(null);
    const r = await enviar<{ resultados: Resultado[]; noEncontradas: number[] }>("/api/alumnos/importacion/buscar", { claves: todas.map(Number), nombre });
    setOcupado(false);
    if (r.error) return setError(r.error);
    setBusqueda(r.datos!);
    setSeleccion(r.datos!.resultados.filter((x) => x.situacion === "listo").map((x) => x.clave));
  }

  async function importar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setOcupado(true);
    setError(null);
    const r = await enviar<Final>("/api/alumnos/importacion", {
      claves: seleccion,
      idGeneracion: Number(f.get("idGeneracion")),
      idPeriodo: Number(f.get("idPeriodo")),
      fechaIngreso: f.get("fechaIngreso"),
      semestre: Number(f.get("semestre")),
    });
    setOcupado(false);
    if (r.error) return setError(r.error);
    setFinal(r.datos!);
    setPaso(2);
  }

  const p = pagina("/alumnos/importar")!;
  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion="Importa alumnos desde el registro de la universidad." />
      <Pasos pasos={["Buscar en el registro", "Datos de ingreso", "Resultado"]} actual={paso} />
      {error && <Alerta tipo="advertencia">{error}</Alerta>}

      {paso === 0 && (
        <>
          <Tarjeta titulo="Buscar en el registro de la universidad" descripcion="Escribe o pega una o varias claves, o busca por nombre.">
            <div className="flex items-start gap-3">
              <div className="flex flex-1 flex-wrap items-center gap-2 rounded-lg border-[1.5px] border-portal-borde-campo px-2.5 py-[9px] focus-within:border-portal-activo">
                {claves.map((c) => (
                  <span key={c} className="flex items-center gap-1 rounded-md bg-portal-neutro-fondo py-[5px] pr-1.5 pl-2.5 text-[13px] font-medium text-portal-menu-texto">
                    {c}
                    <button type="button" onClick={() => setClaves(claves.filter((x) => x !== c))} aria-label={`Quitar ${c}`}>
                      <X size={14} aria-hidden />
                    </button>
                  </span>
                ))}
                <input
                  value={texto}
                  onChange={(e) => (/[\s,;]$/.test(e.target.value) ? agregar(e.target.value) : setTexto(e.target.value))}
                  onPaste={(e) => { e.preventDefault(); agregar(`${texto} ${e.clipboardData.getData("text")}`); }}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") { e.preventDefault(); buscar(); }
                    else if (e.key === "Backspace" && !texto && claves.length) setClaves(claves.slice(0, -1));
                  }}
                  placeholder={claves.length ? "Agrega más claves o un nombre…" : "Claves o nombre"}
                  aria-label="Claves o nombre"
                  className="min-w-48 flex-1 bg-transparent text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder"
                />
              </div>
              <BotonPortal onClick={buscar} disabled={ocupado}>
                <Search size={18} aria-hidden />
                Buscar
              </BotonPortal>
            </div>
          </Tarjeta>

          {busqueda && busqueda.noEncontradas.length > 0 && (
            <Alerta tipo="advertencia">
              {busqueda.noEncontradas.length === 1 ? `No se encontró la clave ${busqueda.noEncontradas[0]}.` : `No se encontraron las claves ${busqueda.noEncontradas.join(", ")}.`}
            </Alerta>
          )}

          {busqueda && busqueda.resultados.length > 0 && (
            <Resultados resultados={busqueda.resultados} seleccion={seleccion} onSeleccion={setSeleccion} onContinuar={() => setPaso(1)} />
          )}
          {busqueda && !busqueda.resultados.length && !busqueda.noEncontradas.length && (
            <Alerta tipo="advertencia">No se encontraron estudiantes con ese nombre.</Alerta>
          )}
        </>
      )}

      {paso === 1 && opciones && (
        <form onSubmit={importar} className="flex flex-col gap-5">
          <Tarjeta titulo="Datos de ingreso" descripcion={`Aplican a ${seleccion.length === 1 ? "el alumno seleccionado" : `los ${seleccion.length} alumnos seleccionados`}.`}>
            <DatosIngreso opciones={opciones} />
          </Tarjeta>
          <div className="flex justify-between">
            <BotonPortal type="button" variante="contorno" onClick={() => setPaso(0)}>
              <ArrowLeft size={18} aria-hidden />
              Atrás
            </BotonPortal>
            <BotonPortal type="submit" disabled={ocupado}>
              Importar {seleccion.length === 1 ? "1 alumno" : `${seleccion.length} alumnos`}
            </BotonPortal>
          </div>
        </form>
      )}

      {paso === 2 && final && <ResultadoFinal final={final} onOtra={reiniciar} />}
    </>
  );
}

function Tarjeta({ titulo, descripcion, children }: { titulo: string; descripcion?: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-5">
      <div className="flex flex-col gap-1">
        <h2 className="text-lg font-semibold text-portal-titulo">{titulo}</h2>
        {descripcion && <p className="text-sm text-portal-descripcion">{descripcion}</p>}
      </div>
      {children}
    </section>
  );
}

const fila = "grid grid-cols-[40px_minmax(0,1fr)_120px_170px_300px] items-center px-3";

function Resultados({ resultados, seleccion, onSeleccion, onContinuar }: {
  resultados: Resultado[];
  seleccion: number[];
  onSeleccion: (s: number[]) => void;
  onContinuar: () => void;
}) {
  const listos = resultados.filter((r) => r.situacion === "listo").map((r) => r.clave);
  const todos = listos.length > 0 && listos.every((c) => seleccion.includes(c));
  return (
    <Tarjeta titulo="Resultados">
      <div>
        <div className={`${fila} rounded-lg bg-portal-tabla-cabecera py-2.5 text-[13px] font-semibold text-portal-subtexto`}>
          <label className="cursor-pointer">
            <input type="checkbox" className="sr-only" checked={todos} disabled={!listos.length} onChange={() => onSeleccion(todos ? [] : listos)} aria-label="Seleccionar todos" />
            <Casilla marcada={todos} />
          </label>
          <span>Alumno</span><span>Clave</span><span>En la universidad</span><span>Situación</span>
        </div>
        {resultados.map((r) => {
          const elegible = r.situacion === "listo";
          const marcada = seleccion.includes(r.clave);
          const s = situaciones[r.situacion];
          return (
            <div key={r.clave} className={`${fila} border-b border-portal-borde py-3 text-sm text-portal-menu-texto`}>
              <label className={elegible ? "cursor-pointer" : "cursor-not-allowed"}>
                <input type="checkbox" className="sr-only" checked={marcada} disabled={!elegible} aria-label={`Seleccionar a ${r.nombre}`}
                  onChange={() => onSeleccion(marcada ? seleccion.filter((c) => c !== r.clave) : [...seleccion, r.clave])} />
                {elegible ? <Casilla marcada={marcada} /> : <span className="block size-[18px] rounded-[5px] border-[1.5px] border-portal-borde-campo bg-portal-neutro-fondo" aria-hidden />}
              </label>
              <span>
                <span className="block font-semibold text-portal-titulo">{r.nombre}</span>
                <span className="mt-[3px] block text-[13px] text-portal-subtexto">{r.correo}</span>
              </span>
              <span>{r.clave}</span>
              <span>{r.estatus ?? "—"}</span>
              <span><Insignia tono={s.tono}>{s.texto}</Insignia></span>
            </div>
          );
        })}
      </div>
      <div className="flex items-center justify-end gap-4 pt-1">
        <span className="text-sm font-medium text-portal-menu-texto">{seleccion.length} de {resultados.length} seleccionados</span>
        <BotonPortal onClick={onContinuar} disabled={!seleccion.length}>
          <ArrowRight size={18} aria-hidden />
          Continuar
        </BotonPortal>
      </div>
    </Tarjeta>
  );
}

function DatosIngreso({ opciones }: { opciones: Opciones }) {
  const [idPeriodo, setIdPeriodo] = useState(opciones.periodos[0]?.id ?? 0);
  // La fecha de ingreso se propone con el inicio del periodo; se puede cambiar.
  const [fecha, setFecha] = useState(opciones.periodos[0]?.fechaInicio?.slice(0, 10) ?? "");
  return (
    <div className="grid grid-cols-3 gap-x-5 gap-y-4">
      <SelectorPortal etiqueta="Generación" name="idGeneracion" required defaultValue={opciones.generaciones[0]?.id ?? ""}>
        {opciones.generaciones.map((g) => <option key={g.id} value={g.id}>{g.nombre}</option>)}
      </SelectorPortal>
      <SelectorPortal etiqueta="Periodo de ingreso" name="idPeriodo" required value={idPeriodo}
        onChange={(e) => {
          const id = Number(e.target.value);
          setIdPeriodo(id);
          setFecha(opciones.periodos.find((x) => x.id === id)?.fechaInicio?.slice(0, 10) ?? fecha);
        }}>
        {opciones.periodos.map((x) => <option key={x.id} value={x.id}>{x.nombre}</option>)}
      </SelectorPortal>
      <CampoPortal etiqueta="Fecha de ingreso" name="fechaIngreso" type="date" required value={fecha} onChange={(e) => setFecha(e.target.value)} />
      <SelectorPortal etiqueta="Semestre" name="semestre" required defaultValue={1}>
        {[1, 2, 3, 4, 5, 6, 7, 8].map((n) => <option key={n} value={n}>{n}</option>)}
      </SelectorPortal>
    </div>
  );
}

function ResultadoFinal({ final, onOtra }: { final: Final; onOtra: () => void }) {
  const [copiado, setCopiado] = useState(false);
  const filas = final.importados.map((i) => [i.nombre, String(i.clave), i.correo, i.contrasena]);
  const encabezados = ["Alumno", "Clave", "Correo institucional", "Contraseña inicial"];

  function copiar() {
    navigator.clipboard.writeText([encabezados, ...filas].map((f) => f.join("\t")).join("\n")).then(() => setCopiado(true));
  }
  function descargarCsv() {
    const csv = [encabezados, ...filas].map((f) => f.map((c) => `"${c.replaceAll('"', '""')}"`).join(",")).join("\r\n");
    // El BOM hace que Excel lea bien los acentos.
    const url = URL.createObjectURL(new Blob(["﻿" + csv], { type: "text/csv;charset=utf-8" }));
    const a = Object.assign(document.createElement("a"), { href: url, download: "credenciales_alumnos.csv" });
    a.click();
    URL.revokeObjectURL(url);
  }

  const n = final.importados.length;
  return (
    <>
      {n > 0 && <Alerta tipo="exito">{n === 1 ? "Se importó 1 alumno." : `Se importaron ${n} alumnos.`}</Alerta>}
      {final.errores.length > 0 && (
        <Alerta tipo="advertencia">
          <p>{final.errores.length === 1 ? "No se importó 1 alumno:" : `No se importaron ${final.errores.length} alumnos:`}</p>
          <ul className="mt-1 list-disc pl-5">
            {final.errores.map((e) => <li key={e.clave}>{e.clave}: {e.mensaje}</li>)}
          </ul>
        </Alerta>
      )}
      {n > 0 && (
        <Tarjeta titulo="Credenciales de acceso" descripcion="Entrega a cada alumno su contraseña inicial.">
          <Alerta tipo="advertencia">Las contraseñas solo se muestran esta vez.</Alerta>
          <table className="w-full text-left text-sm text-portal-menu-texto">
            <thead className="text-[13px] text-portal-subtexto">
              <tr className="bg-portal-tabla-cabecera">
                {encabezados.map((e, i) => <th key={e} className={`py-2.5 font-semibold ${i === 0 ? "rounded-l-lg pl-3" : ""} ${i === 3 ? "rounded-r-lg" : ""}`}>{e}</th>)}
              </tr>
            </thead>
            <tbody>
              {final.importados.map((i) => (
                <tr key={i.clave} className="border-b border-portal-borde">
                  <td className="py-3 pl-3 font-semibold text-portal-titulo">{i.nombre}</td>
                  <td>{i.clave}</td>
                  <td>{i.correo}</td>
                  <td><code className="rounded-md bg-portal-neutro-fondo px-2 py-1 font-sans font-medium text-portal-titulo">{i.contrasena}</code></td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="flex gap-2.5">
            <BotonPortal variante="contorno" onClick={copiar}>
              <Copy size={18} aria-hidden />
              {copiado ? "Copiado" : "Copiar"}
            </BotonPortal>
            <BotonPortal variante="contorno" onClick={descargarCsv}>
              <Download size={18} aria-hidden />
              Descargar CSV
            </BotonPortal>
          </div>
        </Tarjeta>
      )}
      <div className="flex justify-end gap-3">
        <BotonPortal variante="secundario" onClick={onOtra}>Importar más alumnos</BotonPortal>
        <Link href="/alumnos" className="flex items-center gap-2 rounded-lg bg-portal-activo py-2.5 pr-[18px] pl-4 text-[15px] font-semibold text-blanco">
          <ArrowRight size={18} aria-hidden />
          Ir a la consulta
        </Link>
      </div>
    </>
  );
}
