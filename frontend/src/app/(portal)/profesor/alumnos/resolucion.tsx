"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Alerta, BotonPortal, CampoPortal, Dato, Dialogo, SelectorPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { fecha } from "@/app/(portal)/alumnos/[clave]/pestanas";

// Registrar resolución final (Figma PT9): el director cierra el trabajo como Aceptada con el acta que lo respalda.

type Datos = {
  alumno: string;
  clave: number;
  modalidad: string | null;
  titulo: string | null;
  avance: number;
  cerradas: number;
  actas: { cve: string; numero: number | null; fecha: string | null }[];
};

export function DialogoResolucion({ idTesis, onCerrar, onListo }: { idTesis: number | null; onCerrar: () => void; onListo: () => void }) {
  return (
    <Dialogo abierto={idTesis !== null} onCerrar={onCerrar} titulo="Registrar resolución final" ancho="w-[560px]">
      {idTesis !== null && <Formulario idTesis={idTesis} onCerrar={onCerrar} onListo={onListo} />}
    </Dialogo>
  );
}

function Formulario({ idTesis, onCerrar, onListo }: { idTesis: number; onCerrar: () => void; onListo: () => void }) {
  const [datos, setDatos] = useState<Datos | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    fetch(`/api/profesor/alumnos/${idTesis}/resolucion`).then(async (res) => {
      if (res.ok) setDatos(await res.json());
      else setError("El trabajo no está listo para la resolución final.");
    });
  }, [idTesis]);

  async function registrar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setEnviando(true);
    const fallo = await enviarJson(`/api/profesor/alumnos/${idTesis}/resolucion`, "POST", { fecha: f.get("fecha"), cveActa: f.get("acta") });
    setEnviando(false);
    if (fallo) setError(fallo);
    else onListo();
  }

  if (!datos) return error ? <Alerta tipo="error">{error}</Alerta> : null;
  const hoy = new Date().toLocaleDateString("en-CA");
  return (
    <form onSubmit={registrar} className="flex flex-col gap-[18px]">
      <p className="-mt-4 text-sm text-portal-descripcion">{datos.alumno} · {datos.clave}</p>
      <dl className="flex flex-col gap-2.5 rounded-[10px] bg-portal-tabla-cabecera px-4 py-3.5">
        <Dato etiqueta="Trabajo">{datos.modalidad} · {datos.titulo}</Dato>
        <Dato etiqueta="Avance final">{Math.round(datos.avance)} %</Dato>
        <Dato etiqueta="Evaluaciones">{datos.cerradas} {datos.cerradas === 1 ? "cerrada" : "cerradas"}</Dato>
        <Dato etiqueta="Resolución">Aceptada</Dato>
      </dl>
      <CampoPortal etiqueta="Fecha de resolución" name="fecha" type="date" required max={hoy} defaultValue={hoy} />
      <SelectorPortal etiqueta="Acta asociada" name="acta" required defaultValue={datos.actas[0]?.cve ?? ""}>
        <option value="" disabled>Seleccionar acta</option>
        {datos.actas.map((a) => <option key={a.cve} value={a.cve}>Acta del Informe {a.numero} · {fecha(a.fecha)}</option>)}
      </SelectorPortal>
      {error && <Alerta tipo="error">{error}</Alerta>}
      <div className="flex justify-end gap-2.5">
        <BotonPortal type="button" variante="contorno" onClick={onCerrar}>Cancelar</BotonPortal>
        <BotonPortal type="submit" disabled={enviando}>Registrar resolución</BotonPortal>
      </div>
    </form>
  );
}
