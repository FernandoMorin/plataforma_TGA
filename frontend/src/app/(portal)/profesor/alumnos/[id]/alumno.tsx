"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { BarraAvance, Dato, Insignia, type Tono } from "@/components/modulo";
import { ListaAvances, TarjetaInforme, type Avance, type Informe } from "@/components/informe";
import { sesionVencida } from "@/lib/acceso";
import { tonoTitulacion } from "@/app/(portal)/alumnos/consulta";
import { fecha } from "@/app/(portal)/alumnos/[clave]/pestanas";
import { rutaAccion } from "../alumnos";
import { DialogoResolucion } from "../resolucion";

// Detalle de un alumno para su comité: trabajo, comité y sus informes con lo que le toca a cada quien.
// Sin maqueta propia: reúne PT2 y PT4 con las tarjetas de AT5.

type Datos = {
  alumno: string;
  clave: number;
  generacion: string | null;
  modalidad: string | null;
  titulo: string | null;
  linea: string | null;
  estado: string;
  avance: number;
  fechaRegistro: string | null;
  comite: string | null;
  soyDirector: boolean;
  puedeResolver: boolean;
  integrantes: { nombre: string; rol: string; soyYo: boolean }[];
  avances: Avance[];
  informes: { informe: Informe; estadoParaMi: string; tono: Tono; accion: string | null }[];
};

const boton = "inline-flex rounded-lg py-2.5 pr-[18px] pl-4 text-[15px] font-semibold whitespace-nowrap";

export function AlumnoDelComite() {
  const router = useRouter();
  const id = Number(useParams<{ id: string }>().id);
  const [datos, setDatos] = useState<Datos | null>(null);
  const [resolviendo, setResolviendo] = useState(false);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    fetch(`/api/profesor/alumnos/${id}`).then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setDatos(await res.json());
      else router.replace("/profesor/alumnos");
    });
  }, [id, router, version]);

  const volver = <Link href="/profesor/alumnos" className="text-sm font-semibold text-portal-activo">← Mis alumnos</Link>;
  if (!datos) return volver;
  const avancesDe = (numero: number) => datos.avances.filter((d) => d.informe === numero);
  const siguientes = avancesDe(datos.informes.length + 1);
  const completo = datos.integrantes.some((m) => m.rol === "Director") && datos.integrantes.filter((m) => m.rol === "Sinodal").length === 2;

  return (
    <>
      {volver}
      <EncabezadoPagina titulo={datos.alumno} descripcion={`${datos.clave}${datos.generacion ? ` · Generación ${datos.generacion}` : ""}`} />
      <div className="flex items-start gap-5">
        <section className="flex flex-1 flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <div className="flex items-center justify-between">
            <h2 className="text-[17px] font-semibold text-portal-titulo">{datos.modalidad}</h2>
            <Insignia tono={tonoTitulacion[datos.estado] ?? "gris"}>{datos.estado}</Insignia>
          </div>
          <p className="text-lg font-semibold text-portal-titulo">{datos.titulo}</p>
          <dl className="flex flex-col gap-2.5">
            <Dato etiqueta="Línea de investigación">{datos.linea}</Dato>
            <Dato etiqueta="Fecha de registro">{fecha(datos.fechaRegistro)}</Dato>
            <Dato etiqueta="Avance"><BarraAvance porcentaje={datos.avance} /></Dato>
          </dl>
          {datos.puedeResolver && (
            <div className="flex justify-end">
              <button onClick={() => setResolviendo(true)} className={`${boton} bg-portal-activo text-blanco`}>Registrar resolución</button>
            </div>
          )}
        </section>
        <section className="flex w-[380px] flex-col gap-3.5 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <div className="flex items-center justify-between">
            <h2 className="text-[17px] font-semibold text-portal-titulo">Comité</h2>
            {datos.comite && <span className="text-[13px] text-portal-subtexto">{datos.comite}</span>}
          </div>
          {datos.integrantes.length === 0 && <p className="text-sm text-portal-subtexto">Sin armar.</p>}
          <ul>
            {datos.integrantes.map((m) => (
              <li key={m.nombre} className="flex items-center justify-between gap-3 border-t border-portal-borde py-2 text-sm">
                <span className="text-portal-titulo">{m.nombre}{m.soyYo && " (tú)"}</span>
                <Insignia tono={m.rol === "Director" ? "azul" : "gris"}>{m.rol}</Insignia>
              </li>
            ))}
          </ul>
          {datos.soyDirector && !completo && (
            <Link href={rutaAccion(id, "Armar comité", null)} className={`${boton} justify-center border border-portal-borde-campo bg-blanco text-portal-menu-texto`}>Armar comité</Link>
          )}
        </section>
      </div>

      <h2 className="text-lg font-semibold text-portal-titulo">Informes</h2>
      {datos.informes.length === 0 && siguientes.length === 0 && <p className="text-[15px] text-portal-subtexto">Aún no hay informes.</p>}
      {siguientes.length > 0 && (
        <section className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <div className="flex items-center justify-between">
            <h2 className="text-[17px] font-semibold text-portal-titulo">Informe {datos.informes.length + 1}</h2>
            <Insignia tono="gris">Por abrir</Insignia>
          </div>
          <Avances avances={siguientes} />
        </section>
      )}
      {datos.informes.map(({ informe, estadoParaMi, tono, accion }) => (
        <TarjetaInforme key={informe.id} informe={informe}
          insignia={informe.cerrada ? undefined : <Insignia tono={tono}>{estadoParaMi}</Insignia>}
          abierto={
            <div className="flex items-center justify-between">
              <p className="text-sm text-portal-subtexto">Informe en curso.</p>
              {accion && <Link href={rutaAccion(id, accion, informe.id)} className={`${boton} bg-portal-activo text-blanco`}>{accion}</Link>}
            </div>
          }>
          {avancesDe(informe.numero).length > 0 && <Avances avances={avancesDe(informe.numero)} />}
        </TarjetaInforme>
      ))}
      <DialogoResolucion idTesis={resolviendo ? id : null} onCerrar={() => setResolviendo(false)} onListo={() => { setResolviendo(false); setVersion((v) => v + 1); }} />
    </>
  );
}

function Avances({ avances }: { avances: Avance[] }) {
  return (
    <div className="flex flex-col gap-1.5">
      <h3 className="text-sm font-semibold text-portal-titulo">Avances del alumno</h3>
      <ListaAvances avances={avances} />
    </div>
  );
}
