"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState, type ReactNode } from "react";
import { X } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, Casilla, Dato, Insignia } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { sesionVencida } from "@/lib/acceso";
import { tonoToefl } from "@/app/(portal)/alumnos/consulta";
import { fecha } from "@/app/(portal)/alumnos/[clave]/pestanas";

// Consolidar evaluación (Figma PT6): el director fija la calificación final, los comentarios del acta y los compromisos.

const criterios = [
  { clave: "documento", texto: "Documento de avance" },
  { clave: "exposicion", texto: "Exposición" },
  { clave: "dominio", texto: "Dominio del tema" },
  { clave: "gradoAvance", texto: "Grado de avance en el periodo" },
] as const;
type Clave = (typeof criterios)[number]["clave"] | "avance";
type Calificaciones = Record<Clave, number | null>;

type Datos = {
  alumno: string;
  informe: number;
  periodo: string | null;
  integrantes: (Calificaciones & { encabezado: string; nombre: string; esMio: boolean; comentarios: string | null })[];
  final: Calificaciones & { comentarios: string | null };
  informeAnterior: number | null;
  anteriores: { id: number; descripcion: string; fecha: string | null; cumplido: boolean }[];
  nuevos: { descripcion: string; fecha: string | null }[];
  toefl: { estado: string; fecha: string | null; puntaje: number | null };
};

const promedio = (valores: (number | null)[]) => {
  const n = valores.filter((v): v is number => v !== null);
  return n.length ? n.reduce((a, b) => a + b, 0) / n.length : null;
};
const redondo = (v: number | null) => (v === null ? "" : String(Math.round(v)));
const cajaNumero = "w-[90px] rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5 text-[15px] text-portal-titulo outline-none focus:border-portal-activo";
const cajaTexto = "rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5 text-[15px] text-portal-titulo outline-none focus:border-portal-activo";

export function Consolidar() {
  const router = useRouter();
  const { id: tesis, informe: id } = useParams<{ id: string; informe: string }>();
  const [datos, setDatos] = useState<Datos | null>(null);
  const [final, setFinal] = useState<Record<Clave, string>>({ documento: "", exposicion: "", dominio: "", gradoAvance: "", avance: "" });
  const [comentarios, setComentarios] = useState("");
  const [cumplidos, setCumplidos] = useState<number[]>([]);
  const [nuevos, setNuevos] = useState<{ descripcion: string; fecha: string }[]>([]);
  const [aviso, setAviso] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch(`/api/profesor/evaluaciones/${id}/consolidacion`).then(async (res) => {
      if (res.status === 401) return sesionVencida();
      if (!res.ok) return router.replace(`/profesor/alumnos/${tesis}`);
      const d: Datos = await res.json();
      setDatos(d);
      // La propuesta de cada calificación final es el promedio de los integrantes.
      const propuesta = (c: Clave) => d.final[c] !== null ? String(d.final[c]) : redondo(promedio(d.integrantes.map((m) => m[c])));
      setFinal({ documento: propuesta("documento"), exposicion: propuesta("exposicion"), dominio: propuesta("dominio"), gradoAvance: propuesta("gradoAvance"), avance: propuesta("avance") });
      setComentarios(d.final.comentarios ?? "");
      setCumplidos(d.anteriores.filter((a) => a.cumplido).map((a) => a.id));
      setNuevos(d.nuevos.map((n) => ({ descripcion: n.descripcion, fecha: n.fecha?.slice(0, 10) ?? "" })));
    });
  }, [id, tesis, router]);

  async function guardar(enviar: boolean) {
    const numero = (v: string) => (v === "" ? null : Number(v));
    if (nuevos.some((n) => !n.descripcion.trim() || !n.fecha)) return setAviso({ tipo: "error", texto: "Completa la descripción y la fecha de cada compromiso." });
    setEnviando(true);
    const fallo = await enviarJson(`/api/profesor/evaluaciones/${id}/consolidacion`, "PUT", {
      documento: numero(final.documento), exposicion: numero(final.exposicion), dominio: numero(final.dominio),
      gradoAvance: numero(final.gradoAvance), avance: numero(final.avance), comentarios, cumplidos, nuevos, enviar,
    });
    setEnviando(false);
    if (fallo) return setAviso({ tipo: "error", texto: fallo });
    if (enviar) router.push(`/profesor/alumnos/${tesis}`);
    else setAviso({ tipo: "exito", texto: "Cambios guardados." });
  }

  const volver = <Link href={`/profesor/alumnos/${tesis}`} className="text-sm font-semibold text-portal-activo">← {datos?.alumno ?? "Volver"}</Link>;
  if (!datos) return volver;
  const finales = criterios.map((c) => (final[c.clave] === "" ? null : Number(final[c.clave])));

  return (
    <>
      {volver}
      <EncabezadoPagina titulo="Consolidar evaluación" descripcion={`${datos.alumno} · Informe ${datos.informe} · ${datos.periodo ?? ""}`} />

      <Tarjeta titulo="Revisiones del comité" extra={<span className="text-[13px] font-medium text-portal-exito-texto">{datos.integrantes.length} de {datos.integrantes.length} enviadas</span>}>
        <div className="overflow-hidden rounded-[10px] border border-portal-borde">
          <table className="w-full text-left text-sm">
            <thead className="bg-portal-tabla-cabecera text-[13px] text-portal-subtexto">
              <tr>
                <th className="w-[300px] py-2.5 pl-4 font-semibold">Criterio</th>
                {datos.integrantes.map((m) => <th key={m.encabezado} className="font-semibold">{m.encabezado}</th>)}
                <th className="w-[150px] font-semibold">Final</th>
              </tr>
            </thead>
            <tbody className="text-portal-menu-texto">
              {[...criterios, { clave: "avance" as const, texto: "Avance (%)" }].map((c) => (
                <tr key={c.clave} className="border-t border-portal-borde">
                  <td className="py-2 pl-4 font-medium text-portal-titulo">{c.texto}</td>
                  {datos.integrantes.map((m) => <td key={m.encabezado}>{m[c.clave] === null ? "—" : Number(m[c.clave])}</td>)}
                  <td>
                    <input type="number" min={0} max={100} step={c.clave === "avance" ? 0.01 : 1} required aria-label={`${c.texto}, final`}
                      value={final[c.clave]} onChange={(e) => setFinal({ ...final, [c.clave]: e.target.value })} className={cajaNumero} />
                  </td>
                </tr>
              ))}
              <tr className="bg-portal-activo-fondo text-portal-activo">
                <td className="py-3 pl-4 font-semibold">Promedio de la evaluación</td>
                {datos.integrantes.map((m) => <td key={m.encabezado}>{redondo(promedio(criterios.map((c) => m[c.clave]))) || "—"}</td>)}
                <td className="text-base font-bold">{redondo(promedio(finales)) || "—"}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </Tarjeta>

      <Tarjeta titulo="Comentarios de los integrantes">
        {datos.integrantes.filter((m) => !m.esMio).map((m) => (
          <div key={m.encabezado} className="flex flex-col gap-[3px] border-t border-portal-borde py-2 text-sm">
            <p className="font-semibold text-portal-titulo">{m.nombre}</p>
            <p className="text-portal-menu-texto">{m.comentarios}</p>
          </div>
        ))}
      </Tarjeta>

      <Tarjeta titulo="Comentarios y sugerencias" extra={<span className="text-[13px] text-portal-subtexto">Aparecen en el acta</span>}>
        <textarea value={comentarios} onChange={(e) => setComentarios(e.target.value)} maxLength={4000} aria-label="Comentarios y sugerencias"
          placeholder="Escribe los comentarios del comité para el alumno" className={`${cajaTexto} h-[110px] resize-none placeholder:text-portal-placeholder`} />
      </Tarjeta>

      <Tarjeta titulo="Compromisos">
        {datos.informeAnterior !== null && datos.anteriores.length > 0 && (
          <>
            <p className="text-[13px] font-semibold text-portal-subtexto">Del Informe {datos.informeAnterior}</p>
            {datos.anteriores.map((a) => {
              const marcado = cumplidos.includes(a.id);
              return (
                <label key={a.id} className="flex cursor-pointer items-center gap-2.5 text-sm has-focus-visible:outline-2 has-focus-visible:outline-portal-activo">
                  <input type="checkbox" className="sr-only" checked={marcado}
                    onChange={() => setCumplidos(marcado ? cumplidos.filter((x) => x !== a.id) : [...cumplidos, a.id])} />
                  <Casilla marcada={marcado} />
                  <span className="text-portal-menu-texto">{a.descripcion}</span>
                  <span className={`text-[13px] ${marcado ? "text-portal-exito-texto" : "text-portal-subtexto"}`}>{marcado ? "Cumplido" : "Sin cumplir"}</span>
                </label>
              );
            })}
          </>
        )}
        <p className="text-[13px] font-semibold text-portal-subtexto">Nuevos</p>
        {nuevos.map((n, i) => (
          <div key={i} className="flex items-center gap-3">
            <input value={n.descripcion} maxLength={500} aria-label={`Compromiso ${i + 1}`} placeholder="Descripción del compromiso"
              onChange={(e) => setNuevos(nuevos.map((x, j) => (j === i ? { ...x, descripcion: e.target.value } : x)))}
              className={`${cajaTexto} flex-1 placeholder:text-portal-placeholder`} />
            <input type="date" value={n.fecha} aria-label={`Fecha del compromiso ${i + 1}`}
              onChange={(e) => setNuevos(nuevos.map((x, j) => (j === i ? { ...x, fecha: e.target.value } : x)))}
              className={`${cajaTexto} w-[170px]`} />
            <button type="button" onClick={() => setNuevos(nuevos.filter((_, j) => j !== i))} aria-label={`Quitar compromiso ${i + 1}`} className="text-portal-subtexto">
              <X size={18} aria-hidden />
            </button>
          </div>
        ))}
        <button type="button" onClick={() => setNuevos([...nuevos, { descripcion: "", fecha: "" }])} className="self-start text-sm font-semibold text-portal-activo">
          + Agregar compromiso
        </button>
      </Tarjeta>

      <Tarjeta titulo="Requisito TOEFL" extra={<Insignia tono={tonoToefl[datos.toefl.estado] ?? "gris"}>{datos.toefl.estado}</Insignia>}>
        <dl className="flex flex-col gap-3.5">
          <Dato etiqueta="Fecha del examen">{fecha(datos.toefl.fecha)}</Dato>
          <Dato etiqueta="Puntaje">{datos.toefl.puntaje ?? "—"}</Dato>
        </dl>
      </Tarjeta>

      {aviso && <Alerta tipo={aviso.tipo}>{aviso.texto}</Alerta>}
      <div className="flex justify-end gap-2.5">
        <BotonPortal variante="contorno" onClick={() => guardar(false)} disabled={enviando}>Guardar</BotonPortal>
        <BotonPortal onClick={() => guardar(true)} disabled={enviando}>Enviar a firma</BotonPortal>
      </div>
    </>
  );
}

function Tarjeta({ titulo, extra, children }: { titulo: string; extra?: ReactNode; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-6 py-5">
      <div className="flex items-center justify-between">
        <h2 className="text-[17px] font-semibold text-portal-titulo">{titulo}</h2>
        {extra}
      </div>
      {children}
    </section>
  );
}
