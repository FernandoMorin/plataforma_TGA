"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { Eye, Search } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { FiltroMultiple, Insignia } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";
import { pagina } from "@/lib/menu";
import { DialogoResolucion } from "./resolucion";

// Mis alumnos del profesor (Figma PT2, con filtros y acceso al detalle de cada alumno, que sustituye a PT4).

type Fila = {
  id: number;
  clave: number;
  alumno: string;
  generacion: string | null;
  modalidad: string | null;
  miRol: string | null;
  comite: "Completo" | "Incompleto" | "Sin armar";
  avance: number;
  accion: "Armar comité" | "Revisar" | "Consolidar" | "Firmar" | "Registrar resolución" | null;
  idRevision: number | null;
};

export const rutaAccion = (id: number, accion: string, idRevision: number | null) => ({
  "Armar comité": `/profesor/alumnos/${id}/comite`,
  Revisar: `/profesor/alumnos/${id}/informe/${idRevision}`,
  Consolidar: `/profesor/alumnos/${id}/informe/${idRevision}/consolidar`,
  Firmar: `/profesor/alumnos/${id}/informe/${idRevision}/firmar`,
  "Ver acta": `/profesor/alumnos/${id}/informe/${idRevision}/firmar`,
})[accion]!;

const boton = "inline-flex rounded-lg py-2.5 pr-[18px] pl-4 text-[15px] font-semibold whitespace-nowrap";
const normalizar = (s: string) => s.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase();
const PENDIENTE = "Con pendiente", SIN_PENDIENTE = "Sin pendiente";

export function MisAlumnos() {
  const [filas, setFilas] = useState<Fila[] | null>(null);
  const [busqueda, setBusqueda] = useState("");
  const [roles, setRoles] = useState<string[]>([]);
  const [pendientes, setPendientes] = useState<string[]>([]);
  const [comites, setComites] = useState<string[]>([]);
  const [resolviendo, setResolviendo] = useState<number | null>(null);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    fetch("/api/profesor/alumnos").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setFilas(await res.json());
    });
  }, [version]);

  const { visibles, opciones } = useMemo(() => {
    const todas = filas ?? [];
    const texto = normalizar(busqueda.trim());
    const pendiente = (f: Fila) => (f.accion ? PENDIENTE : SIN_PENDIENTE);
    const pasa = (f: Fila) => (!texto || normalizar(`${f.alumno} ${f.clave}`).includes(texto))
      && (!roles.length || roles.includes(f.miRol ?? ""))
      && (!pendientes.length || pendientes.includes(pendiente(f)))
      && (!comites.length || comites.includes(f.comite));
    const conteo = (valores: string[], de: (f: Fila) => string) => valores.map((valor) => ({ valor, conteo: todas.filter((f) => de(f) === valor).length }));
    return {
      visibles: todas.filter(pasa),
      opciones: {
        roles: conteo(["Director", "Codirector", "Sinodal"], (f) => f.miRol ?? ""),
        pendientes: conteo([PENDIENTE, SIN_PENDIENTE], pendiente),
        comites: conteo(["Completo", "Incompleto", "Sin armar"], (f) => f.comite),
      },
    };
  }, [filas, busqueda, roles, pendientes, comites]);

  const p = pagina("/profesor/alumnos")!;
  return (
    <>
      <EncabezadoPagina titulo={p.titulo} descripcion={p.descripcion} />
      <section className="rounded-xl border border-portal-borde bg-blanco">
        <div className="flex flex-wrap items-center gap-2.5 px-5 py-4">
          <label className="flex w-[330px] items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5">
            <Search size={18} className="text-portal-placeholder" aria-hidden />
            <input type="text" value={busqueda} onChange={(e) => setBusqueda(e.target.value)}
              placeholder="Buscar por nombre o clave" aria-label="Buscar por nombre o clave"
              className="flex-1 text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder" />
          </label>
          <FiltroMultiple etiqueta="Mi rol" titulo="Mi rol en el comité" opciones={opciones.roles} seleccion={roles} onAplicar={setRoles} />
          <FiltroMultiple etiqueta="Pendiente" titulo="Pendiente" opciones={opciones.pendientes} seleccion={pendientes} onAplicar={setPendientes} />
          <FiltroMultiple etiqueta="Comité" titulo="Estado del comité" opciones={opciones.comites} seleccion={comites} onAplicar={setComites} />
        </div>
        <table className="w-full table-fixed border-t border-portal-borde text-left">
          <colgroup><col className="w-[320px]" /><col className="w-[170px]" /><col className="w-[130px]" /><col className="w-[140px]" /><col className="w-[100px]" /><col /><col className="w-[70px]" /></colgroup>
          <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
            <tr>
              <th className="py-2.5 pl-5 font-semibold">Alumno</th>
              <th className="font-semibold">Modalidad</th>
              <th className="font-semibold">Mi rol</th>
              <th className="font-semibold">Comité</th>
              <th className="font-semibold">Avance</th>
              <th className="font-semibold">Pendiente</th>
              <th><span className="sr-only">Ver detalle</span></th>
            </tr>
          </thead>
          <tbody className="text-sm text-portal-menu-texto">
            {filas && visibles.length === 0 && (
              <tr className="border-t border-portal-borde">
                <td colSpan={7} className="py-4 pl-5 text-portal-placeholder">{filas.length === 0 ? "Aún no participas en ningún comité." : "Ningún alumno coincide con los filtros."}</td>
              </tr>
            )}
            {visibles.map((f) => (
              <tr key={f.id} className="border-t border-portal-borde">
                <td className="py-3 pl-5">
                  <Link href={`/profesor/alumnos/${f.id}`} className="font-semibold text-portal-titulo hover:underline">{f.alumno}</Link>
                  <p className="mt-[3px] text-[13px] text-portal-subtexto">{f.clave}{f.generacion && ` · Generación ${f.generacion}`}</p>
                </td>
                <td>{f.modalidad}</td>
                <td>{f.miRol && <Insignia tono="gris">{f.miRol}</Insignia>}</td>
                <td>{f.comite === "Completo" ? "Completo" : <Insignia tono="rojo">{f.comite}</Insignia>}</td>
                <td>{Math.round(f.avance)} %</td>
                <td className="py-2">
                  {f.accion === "Registrar resolución" ? (
                    <button onClick={() => setResolviendo(f.id)} className={`${boton} bg-portal-activo text-blanco`}>{f.accion}</button>
                  ) : f.accion && (
                    <Link href={rutaAccion(f.id, f.accion, f.idRevision)} className={`${boton} border border-portal-borde-campo bg-blanco text-portal-menu-texto`}>
                      {f.accion}
                    </Link>
                  )}
                </td>
                <td className="pr-5 text-right">
                  <Link href={`/profesor/alumnos/${f.id}`} aria-label={`Ver detalle de ${f.alumno}`} className="inline-flex size-[34px] items-center justify-center rounded-lg border border-portal-borde-campo text-portal-activo">
                    <Eye size={18} aria-hidden />
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
      <DialogoResolucion idTesis={resolviendo} onCerrar={() => setResolviendo(null)} onListo={() => { setResolviendo(null); setVersion((v) => v + 1); }} />
    </>
  );
}
