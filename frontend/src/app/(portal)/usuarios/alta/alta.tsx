"use client";

import Link from "next/link";
import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, CampoPortal, SelectorPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import type { Opciones } from "../../alumnos/[clave]/dialogos";

// Alta de usuario (Figma U2, U2b, U2c; coordinador y administrador sin maqueta propia).

const tipos = [
  { valor: "profesor", titulo: "Profesor", detalle: "Plantilla de la maestría", tarjeta: "Datos del profesor", boton: "Registrar profesor" },
  { valor: "externo", titulo: "Profesor externo", detalle: "Invitado a un comité", tarjeta: "Datos del profesor externo", boton: "Registrar profesor externo" },
  { valor: "alumno", titulo: "Alumno temporal", detalle: "Aún no está en el registro", tarjeta: "Datos del alumno", boton: "Registrar alumno" },
  { valor: "coordinador", titulo: "Coordinador", tarjeta: "Datos del coordinador", boton: "Registrar coordinador" },
  { valor: "administrador", titulo: "Administrador", tarjeta: "Datos del administrador", boton: "Registrar administrador" },
] as const;
type Tipo = (typeof tipos)[number]["valor"];

export function AltaUsuario() {
  const [tipo, setTipo] = useState<Tipo>("profesor");
  const [opciones, setOpciones] = useState<Opciones | null>(null);
  const [claveExterno, setClaveExterno] = useState<string | null>(null);
  const [resultado, setResultado] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);
  // Se incrementa tras cada alta para limpiar el formulario y pedir la siguiente clave EXT.
  const [version, setVersion] = useState(0);
  const t = tipos.find((x) => x.valor === tipo)!;

  useEffect(() => {
    fetch("/api/alumnos/opciones").then(async (res) => res.ok && setOpciones(await res.json()));
  }, []);
  useEffect(() => {
    fetch("/api/usuarios/siguiente-externo").then(async (res) => res.ok && setClaveExterno((await res.json()).clave));
  }, [version]);

  async function registrar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = Object.fromEntries(new FormData(e.currentTarget)) as Record<string, string>;
    const numero = (v?: string) => (v ? Number(v) : null);
    setEnviando(true);
    const error = await enviarJson("/api/usuarios", "POST", {
      tipo, correo: f.correo, nombre: f.nombre, apellidoPaterno: f.apellidoPaterno, apellidoMaterno: f.apellidoMaterno || null,
      rpe: f.rpe || null, institucion: f.institucion || null, vigenteHasta: f.vigenteHasta || null,
      claveAlumno: numero(f.claveAlumno), idGeneracion: numero(f.idGeneracion), idPeriodo: numero(f.idPeriodo),
    });
    setEnviando(false);
    if (error) return setResultado({ tipo: "error", texto: error });
    setResultado({ tipo: "exito", texto: `Se registró a ${f.nombre} ${f.apellidoPaterno}. Su contraseña inicial se envió a ${f.correo}.` });
    setVersion((v) => v + 1);
  }

  return (
    <>
      <EncabezadoPagina titulo="Alta de usuario" />
      <div className="mx-auto flex w-full max-w-[880px] flex-col gap-5">
        <div role="radiogroup" aria-label="Tipo de cuenta" className="grid grid-cols-5 gap-3">
          {tipos.map((x) => {
            const elegido = x.valor === tipo;
            return (
              <button key={x.valor} type="button" role="radio" aria-checked={elegido}
                onClick={() => { setTipo(x.valor); setResultado(null); }}
                className={`flex flex-col gap-0.5 rounded-[10px] px-[15px] py-3.5 text-left ${elegido ? "border-[1.5px] border-portal-activo bg-portal-activo-fondo" : "border border-portal-borde bg-blanco"}`}>
                <span className={`text-sm font-semibold ${elegido ? "text-portal-activo" : "text-portal-titulo"}`}>{x.titulo}</span>
                {"detalle" in x && <span className="text-xs text-portal-subtexto">{x.detalle}</span>}
              </button>
            );
          })}
        </div>

        {resultado && <Alerta tipo={resultado.tipo}>{resultado.texto}</Alerta>}

        <form key={`${tipo}-${version}`} onSubmit={registrar} className="flex flex-col gap-5 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <h2 className="text-lg font-semibold text-portal-titulo">{t.tarjeta}</h2>
          <div className="grid grid-cols-2 gap-x-5 gap-y-4">
            <Campos tipo={tipo} opciones={opciones} />
          </div>
          {tipo === "externo" && (
            <div className="flex items-center gap-4 rounded-lg bg-portal-activo-fondo px-4 py-3 text-sm">
              <span className="text-portal-subtexto">Clave asignada</span>
              <span className="text-base font-bold text-portal-activo">{claveExterno ?? "—"}</span>
            </div>
          )}
          {tipo === "alumno" ? (
            <Alerta tipo="advertencia">Quedará pendiente de conciliar con el registro de la universidad.</Alerta>
          ) : (
            tipo !== "externo" && <Alerta tipo="info">La contraseña inicial se genera y se envía a su correo al guardar.</Alerta>
          )}
          <div className="flex justify-end gap-2.5">
            <Link href="/usuarios" className="rounded-lg border border-portal-borde-campo bg-blanco py-2.5 pr-[18px] pl-4 text-[15px] font-semibold text-portal-menu-texto">Cancelar</Link>
            <BotonPortal type="submit" disabled={enviando}>{t.boton}</BotonPortal>
          </div>
        </form>
      </div>
    </>
  );
}

function Campos({ tipo, opciones }: { tipo: Tipo; opciones: Opciones | null }) {
  const nombre = <CampoPortal key="n" etiqueta="Nombre(s)" name="nombre" required maxLength={50} />;
  const paterno = <CampoPortal key="p" etiqueta="Apellido paterno" name="apellidoPaterno" required maxLength={50} />;
  const materno = <CampoPortal key="m" etiqueta="Apellido materno" name="apellidoMaterno" maxLength={50} />;
  const correo = (etiqueta: string) => <CampoPortal key="c" etiqueta={etiqueta} name="correo" type="email" required maxLength={100} />;
  const campos: Record<Tipo, ReactNode[]> = {
    profesor: [<CampoPortal key="r" etiqueta="RPE" name="rpe" required inputMode="numeric" pattern="\d{1,10}" maxLength={10} />, correo("Correo institucional"), nombre, paterno, materno],
    coordinador: [<CampoPortal key="r" etiqueta="RPE" name="rpe" required inputMode="numeric" pattern="\d{1,10}" maxLength={10} />, correo("Correo institucional"), nombre, paterno, materno],
    externo: [nombre, paterno, materno, correo("Correo"),
      <CampoPortal key="i" etiqueta="Institución de procedencia" name="institucion" required maxLength={150} />,
      <CampoPortal key="v" etiqueta="Vigente hasta" name="vigenteHasta" type="date" required min={new Date().toISOString().slice(0, 10)} />],
    alumno: [<CampoPortal key="k" etiqueta="Clave del alumno" name="claveAlumno" required inputMode="numeric" pattern="\d{6}" maxLength={6} />,
      correo("Correo institucional"), nombre, paterno, materno,
      <SelectorPortal key="g" etiqueta="Generación" name="idGeneracion" required defaultValue="">
        <option value="" disabled>Seleccionar</option>
        {opciones?.generaciones.map((g) => <option key={g.id} value={g.id}>{g.nombre}</option>)}
      </SelectorPortal>,
      <SelectorPortal key="pe" etiqueta="Periodo de ingreso" name="idPeriodo" required defaultValue="">
        <option value="" disabled>Seleccionar</option>
        {opciones?.periodos.map((x) => <option key={x.id} value={x.id}>{x.nombre}</option>)}
      </SelectorPortal>],
    administrador: [nombre, paterno, materno, correo("Correo")],
  };
  return <>{campos[tipo]}</>;
}
