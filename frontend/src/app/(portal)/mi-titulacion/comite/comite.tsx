"use client";

import { useEffect, useState } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Insignia } from "@/components/modulo";
import { sesionVencida } from "@/lib/acceso";
import { fecha } from "../../alumnos/[clave]/pestanas";

// Mi comité (Figma AT4): el director resaltado y los demás integrantes.

type Datos = { clave: string | null; registrado: string | null; integrantes: { nombre: string; rol: string; correo: string | null }[] };

const iniciales = (nombre: string) => nombre.split(" ").slice(0, 2).map((p) => p[0]).join("").toUpperCase();

export function MiComite() {
  const [datos, setDatos] = useState<Datos | null>(null);

  useEffect(() => {
    fetch("/api/mi-titulacion/comite").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
    });
  }, []);

  if (!datos) return <EncabezadoPagina titulo="Mi comité" />;
  if (!datos.clave) {
    return (
      <>
        <EncabezadoPagina titulo="Mi comité" />
        <p className="text-[15px] text-portal-subtexto">Tu director aún no registra tu comité.</p>
      </>
    );
  }
  return (
    <>
      <EncabezadoPagina titulo="Mi comité" descripcion={`${datos.clave} · Registrado el ${fecha(datos.registrado)}`} />
      <ul className="mx-auto flex w-[740px] flex-wrap gap-5">
        {datos.integrantes.map((m) => {
          const director = m.rol === "Director";
          return (
            <li key={m.nombre} className={`flex w-[360px] flex-col items-center gap-3 rounded-[14px] border p-[22px] ${director ? "border-portal-info-borde bg-portal-activo-fondo" : "border-portal-borde bg-blanco"}`}>
              <span className={`flex size-16 items-center justify-center rounded-full text-xl font-bold ${director ? "bg-portal-activo text-blanco" : "bg-portal-neutro-fondo text-portal-subtexto"}`} aria-hidden>
                {iniciales(m.nombre)}
              </span>
              <p className="font-semibold text-portal-titulo">{m.nombre}</p>
              <Insignia tono={director ? "azul" : "gris"}>{m.rol}</Insignia>
              {m.correo && <a href={`mailto:${m.correo}`} className="text-sm text-portal-activo">{m.correo}</a>}
            </li>
          );
        })}
      </ul>
    </>
  );
}
