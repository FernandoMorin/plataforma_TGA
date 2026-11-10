"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Search } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, CampoPortal, Insignia, SelectorPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import type { Opciones } from "../../alumnos/[clave]/dialogos";

// Alta de egresado (Figma E4): se busca la clave y se registran sus datos de egreso; la contraseña va a su correo personal.

type Encontrado = { clave: number; nombre: string; correo: string | null; idGeneracion: number | null; origen: string };

export function AltaEgresado() {
  const [opciones, setOpciones] = useState<Opciones | null>(null);
  const [clave, setClave] = useState("");
  const [encontrado, setEncontrado] = useState<Encontrado | null>(null);
  const [generacion, setGeneracion] = useState("");
  const [identificador, setIdentificador] = useState<string | null>(null);
  const [aviso, setAviso] = useState<{ tipo: "exito" | "error"; texto: string } | null>(null);
  const [enviando, setEnviando] = useState(false);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    fetch("/api/alumnos/opciones").then(async (res) => res.ok && setOpciones(await res.json()));
  }, []);
  useEffect(() => {
    if (!encontrado || !generacion) return;
    fetch(`/api/egresados/alta/identificador/${encontrado.clave}/${generacion}`)
      .then(async (res) => setIdentificador(res.ok ? (await res.json()).identificador : null));
  }, [encontrado, generacion]);

  async function buscar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setAviso(null);
    setEncontrado(null);
    setIdentificador(null);
    const res = await fetch(`/api/egresados/alta/buscar/${encodeURIComponent(clave.trim())}`);
    if (!res.ok) return setAviso({ tipo: "error", texto: (await res.json().catch(() => ({}))).mensaje ?? "Escribe una clave de 6 dígitos." });
    const datos: Encontrado = await res.json();
    setEncontrado(datos);
    setGeneracion(datos.idGeneracion ? String(datos.idGeneracion) : "");
  }

  async function registrar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = new FormData(e.currentTarget);
    setEnviando(true);
    const fallo = await enviarJson("/api/egresados/alta", "POST", {
      clave: encontrado!.clave, idGeneracion: Number(generacion), fechaEgreso: f.get("fechaEgreso"), correo: f.get("correo"), telefono: f.get("telefono") || null,
    });
    setEnviando(false);
    if (fallo) return setAviso({ tipo: "error", texto: fallo });
    setAviso({ tipo: "exito", texto: `Se registró a ${encontrado!.nombre} con el identificador ${identificador}. Su contraseña inicial se envió a ${f.get("correo")}.` });
    setEncontrado(null);
    setClave("");
    setVersion((v) => v + 1);
  }

  const hoy = new Date().toLocaleDateString("en-CA");
  return (
    <>
      <EncabezadoPagina titulo="Alta de egresado" descripcion="Registra a egresados de generaciones anteriores al sistema." />
      <div className="mx-auto flex w-[760px] flex-col gap-5">
        {aviso && <Alerta tipo={aviso.tipo}>{aviso.texto}</Alerta>}
        <form onSubmit={buscar} className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
          <h2 className="text-[17px] font-semibold text-portal-titulo">Buscar en el registro de la universidad</h2>
          <div className="flex gap-3">
            <label className="flex flex-1 items-center gap-2 rounded-lg border border-portal-borde-campo bg-blanco px-3 py-2.5">
              <Search size={18} className="text-portal-placeholder" aria-hidden />
              <input value={clave} onChange={(e) => setClave(e.target.value)} required inputMode="numeric" pattern="\d{6}" maxLength={6}
                placeholder="Clave del alumno" aria-label="Clave del alumno"
                className="flex-1 text-[15px] text-portal-titulo outline-none placeholder:text-portal-placeholder" />
            </label>
            <BotonPortal type="submit"><Search size={18} aria-hidden />Buscar</BotonPortal>
          </div>
          {encontrado && (
            <div className="flex items-center gap-3 rounded-[10px] bg-portal-tabla-cabecera px-4 py-3.5">
              <div className="flex flex-1 flex-col gap-[3px]">
                <p className="text-sm font-semibold text-portal-titulo">{encontrado.nombre}</p>
                <p className="text-[13px] text-portal-subtexto">{encontrado.clave}{encontrado.correo && ` · ${encontrado.correo}`}</p>
              </div>
              <Insignia tono="verde">{encontrado.origen}</Insignia>
            </div>
          )}
        </form>

        {encontrado && (
          <form key={`${encontrado.clave}-${version}`} onSubmit={registrar} className="flex flex-col gap-4 rounded-xl border border-portal-borde bg-blanco px-6 py-[22px]">
            <h2 className="text-[17px] font-semibold text-portal-titulo">Datos de egreso</h2>
            <div className="grid grid-cols-2 gap-x-5 gap-y-4">
              <SelectorPortal etiqueta="Generación" required value={generacion} onChange={(e) => setGeneracion(e.target.value)}>
                <option value="" disabled>Seleccionar</option>
                {opciones?.generaciones.map((g) => <option key={g.id} value={g.id}>{g.nombre}</option>)}
              </SelectorPortal>
              <CampoPortal etiqueta="Fecha de egreso" name="fechaEgreso" type="date" required max={hoy} />
              <CampoPortal etiqueta="Correo personal" name="correo" type="email" required maxLength={100} />
              <CampoPortal etiqueta="Teléfono" name="telefono" type="tel" maxLength={20} placeholder="Ej. 444 123 4567" />
            </div>
            {identificador && (
              <div className="flex items-center gap-3 rounded-[10px] bg-portal-activo-fondo px-4 py-3">
                <span className="text-sm text-portal-subtexto">Identificador de egresado</span>
                <span className="font-bold text-portal-activo">{identificador}</span>
              </div>
            )}
            <div className="flex justify-end">
              <BotonPortal type="submit" disabled={enviando || !generacion}>Registrar egresado</BotonPortal>
            </div>
          </form>
        )}
      </div>
    </>
  );
}
