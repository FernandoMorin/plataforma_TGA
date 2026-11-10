"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState, type FormEvent } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, AreaTexto, BotonPortal, CampoPortal, Dato, Insignia } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { sesionVencida } from "@/lib/acceso";
import { desdeUtc, fechaLocal } from "@/lib/fechas";

// Mi revisión (Figma PT5): calificaciones del integrante para el informe. Enviada, queda de solo lectura.

type Mia = { documento: number; exposicion: number; dominio: number; gradoAvance: number; avance: number; comentarios: string; fechaEnvio: string };
type Datos = {
  alumno: string;
  informe: number;
  periodo: string | null;
  modalidad: string | null;
  titulo: string | null;
  miRol: string;
  estado: string;
  enviadas: number;
  integrantes: number;
  evidencias: { id: number; nombre: string | null }[];
  mia: Mia | null;
};

const criterios = [
  { name: "documento", etiqueta: "Documento de avance" },
  { name: "exposicion", etiqueta: "Exposición" },
  { name: "dominio", etiqueta: "Dominio del tema" },
  { name: "gradoAvance", etiqueta: "Grado de avance en el periodo" },
  { name: "avance", etiqueta: "Avance observado (%)" },
] as const;

export function MiRevision() {
  const router = useRouter();
  const { id: tesis, informe: id } = useParams<{ id: string; informe: string }>();
  const [datos, setDatos] = useState<Datos | null>(null);
  const [version, setVersion] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch(`/api/profesor/evaluaciones/${id}`).then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
      else router.replace(`/profesor/alumnos/${tesis}`);
    });
  }, [id, tesis, router, version]);

  async function enviar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = Object.fromEntries(new FormData(e.currentTarget)) as Record<string, string>;
    setEnviando(true);
    const fallo = await enviarJson(`/api/profesor/evaluaciones/${id}/revision`, "POST", {
      documento: Number(f.documento), exposicion: Number(f.exposicion), dominio: Number(f.dominio),
      gradoAvance: Number(f.gradoAvance), avance: Number(f.avance), comentarios: f.comentarios,
    });
    setEnviando(false);
    setError(fallo);
    if (!fallo) setVersion((v) => v + 1);
  }

  const volver = <Link href={`/profesor/alumnos/${tesis}`} className="text-sm font-semibold text-portal-activo">← {datos?.alumno ?? "Volver"}</Link>;
  if (!datos) return volver;
  const mia = datos.mia;
  const abierta = datos.estado === "Abierta";

  return (
    <>
      {volver}
      <EncabezadoPagina titulo="Mi revisión" descripcion={`${datos.alumno} · Informe ${datos.informe} · ${datos.periodo ?? ""}`} />
      <section className="flex flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-6 py-5">
        <div className="flex items-center justify-between">
          <h2 className="text-[17px] font-semibold text-portal-titulo">Trabajo</h2>
          <Insignia tono="gris">{datos.miRol}</Insignia>
        </div>
        <dl className="flex flex-col gap-3.5">
          <Dato etiqueta={datos.modalidad ?? "Trabajo"}>{datos.titulo}</Dato>
          <Dato etiqueta="Evidencia">
            {datos.evidencias.length === 0 ? "—" : (
              <span className="flex flex-wrap gap-x-4 gap-y-1">
                {datos.evidencias.map((d) => <a key={d.id} href={`/api/documentos/${d.id}`} className="font-semibold text-portal-activo">{d.nombre}</a>)}
              </span>
            )}
          </Dato>
          <Dato etiqueta="Revisiones enviadas">{datos.enviadas} de {datos.integrantes}</Dato>
        </dl>
      </section>

      <form onSubmit={enviar} className="flex flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-6 py-5">
        <div className="flex items-center justify-between">
          <h2 className="text-[17px] font-semibold text-portal-titulo">Tu revisión</h2>
          <p className="text-[13px] text-portal-subtexto">Calificaciones de 0 a 100</p>
        </div>
        {/* Se vuelve a montar al enviar para mostrar lo guardado. */}
        <fieldset key={mia?.fechaEnvio ?? "nueva"} disabled={!!mia || !abierta} className="flex flex-col gap-3.5">
          <div className="flex flex-wrap gap-x-5 gap-y-3.5">
            {criterios.map((c) => (
              <div key={c.name} className="w-[250px]">
                <CampoPortal etiqueta={c.etiqueta} name={c.name} type="number" required min={0} max={100} step={c.name === "avance" ? 0.01 : 1}
                  defaultValue={mia?.[c.name] ?? ""} />
              </div>
            ))}
          </div>
          <AreaTexto etiqueta="Comentarios y sugerencias" name="comentarios" required maxLength={4000} defaultValue={mia?.comentarios ?? ""} />
        </fieldset>
        {error && <Alerta tipo="error">{error}</Alerta>}
        {mia ? (
          <Alerta tipo="exito">Enviaste tu revisión el {fechaLocal(desdeUtc(mia.fechaEnvio))}.</Alerta>
        ) : abierta ? (
          <div className="flex justify-end">
            <BotonPortal type="submit" disabled={enviando}>Enviar revisión</BotonPortal>
          </div>
        ) : (
          <Alerta tipo="info">El informe ya no recibe revisiones.</Alerta>
        )}
      </form>
    </>
  );
}
