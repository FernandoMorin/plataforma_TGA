"use client";

import { useEffect, useState, type FormEvent } from "react";
import { Plus } from "lucide-react";
import { EncabezadoPagina } from "@/components/portal";
import { Alerta, BotonPortal, CampoPortal, Casilla, Dialogo, SelectorPortal } from "@/components/modulo";
import { enviarJson } from "@/components/cuenta";
import { TarjetaEmpleo, type Empleo } from "@/components/egresado";
import { sesionVencida } from "@/lib/acceso";

// Historial laboral del egresado (Figma G2) y agregar o editar empleo (G3).

const enlace = "text-[13px] font-semibold";

export function HistorialLaboral() {
  const [empleos, setEmpleos] = useState<Empleo[] | null>(null);
  const [version, setVersion] = useState(0);
  const [editando, setEditando] = useState<Empleo | "nuevo" | null>(null);
  const [eliminando, setEliminando] = useState<Empleo | null>(null);

  useEffect(() => {
    fetch("/api/egresado/empleos").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setEmpleos(await res.json());
    });
  }, [version]);

  async function eliminar() {
    await enviarJson(`/api/egresado/empleos/${eliminando!.id}`, "DELETE", {});
    setEliminando(null);
    setVersion((v) => v + 1);
  }

  return (
    <>
      <EncabezadoPagina titulo="Historial laboral" />
      <div className="mx-auto flex w-[760px] flex-col gap-5">
        <div className="flex justify-end">
          <BotonPortal onClick={() => setEditando("nuevo")}><Plus size={18} aria-hidden />Agregar empleo</BotonPortal>
        </div>
        {empleos?.length === 0 && <p className="text-[15px] text-portal-subtexto">Aún no registras empleos.</p>}
        {empleos?.map((e) => (
          <TarjetaEmpleo key={e.id} empleo={e} acciones={
            <>
              <button onClick={() => setEditando(e)} className={`${enlace} text-portal-activo`}>Editar</button>
              <button onClick={() => setEliminando(e)} className={`${enlace} text-portal-error-texto`}>Eliminar</button>
            </>
          } />
        ))}
      </div>
      <DialogoEmpleo empleo={editando} onCerrar={() => setEditando(null)} onListo={() => { setEditando(null); setVersion((v) => v + 1); }} />
      <Dialogo abierto={eliminando !== null} onCerrar={() => setEliminando(null)} titulo="Eliminar empleo"
        subtitulo={eliminando ? `${eliminando.puesto} · ${eliminando.empresa}` : undefined} ancho="w-[460px]">
        <p className="text-sm text-portal-menu-texto">Se quitará de tu historial laboral.</p>
        <div className="flex justify-end gap-2.5">
          <BotonPortal variante="contorno" onClick={() => setEliminando(null)}>Cancelar</BotonPortal>
          <BotonPortal className="bg-portal-error-texto" onClick={eliminar}>Eliminar</BotonPortal>
        </div>
      </Dialogo>
    </>
  );
}

function DialogoEmpleo({ empleo, onCerrar, onListo }: { empleo: Empleo | "nuevo" | null; onCerrar: () => void; onListo: () => void }) {
  return (
    <Dialogo abierto={empleo !== null} onCerrar={onCerrar} titulo={empleo === "nuevo" ? "Agregar empleo" : "Editar empleo"} ancho="w-[720px]">
      {empleo !== null && <FormEmpleo empleo={empleo === "nuevo" ? null : empleo} onCerrar={onCerrar} onListo={onListo} />}
    </Dialogo>
  );
}

function FormEmpleo({ empleo, onCerrar, onListo }: { empleo: Empleo | null; onCerrar: () => void; onListo: () => void }) {
  const [actual, setActual] = useState(empleo ? empleo.vigente && !empleo.fin : true);
  const [relacion, setRelacion] = useState<boolean>(empleo?.relacion ?? true);
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);
  const hoy = new Date().toLocaleDateString("en-CA");

  async function guardar(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    const f = Object.fromEntries(new FormData(e.currentTarget)) as Record<string, string>;
    setEnviando(true);
    const fallo = await enviarJson(empleo ? `/api/egresado/empleos/${empleo.id}` : "/api/egresado/empleos", empleo ? "PUT" : "POST", {
      empresa: f.empresa, puesto: f.puesto, area: f.area || null, tipo: f.tipo, inicio: f.inicio,
      fin: actual ? null : f.fin || null, relacion, sueldo: f.sueldo ? Number(f.sueldo) : null,
    });
    setEnviando(false);
    if (fallo) setError(fallo);
    else onListo();
  }

  return (
    <form onSubmit={guardar} className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-x-5 gap-y-4">
        <CampoPortal etiqueta="Empresa o institución" name="empresa" required maxLength={150} defaultValue={empleo?.empresa ?? ""} />
        <CampoPortal etiqueta="Puesto" name="puesto" required maxLength={100} defaultValue={empleo?.puesto ?? ""} />
        <CampoPortal etiqueta="Área profesional" name="area" maxLength={100} defaultValue={empleo?.area ?? ""} />
        <SelectorPortal etiqueta="Tipo" name="tipo" required defaultValue={empleo?.tipo ?? "Empleo"}>
          <option value="Empleo">Empleo</option>
          <option value="Beca">Beca</option>
          <option value="Otro">Otro</option>
        </SelectorPortal>
        <CampoPortal etiqueta="Fecha de inicio" name="inicio" type="date" required max={hoy} defaultValue={empleo?.inicio?.slice(0, 10) ?? ""} />
        <CampoPortal etiqueta="Fecha de fin" name="fin" type="date" disabled={actual} required={!actual} defaultValue={empleo?.fin?.slice(0, 10) ?? ""} />
      </div>
      <label className="flex cursor-pointer items-center gap-2 text-sm text-portal-menu-texto has-focus-visible:outline-2 has-focus-visible:outline-portal-activo">
        <input type="checkbox" className="sr-only" checked={actual} onChange={() => setActual(!actual)} />
        <Casilla marcada={actual} />
        Es mi trabajo actual
      </label>
      <fieldset className="flex flex-col gap-2">
        <legend className="mb-2 text-sm font-semibold text-portal-menu-texto">¿Se relaciona con la maestría?</legend>
        <div className="flex gap-3">
          {[true, false].map((v) => {
            const elegido = relacion === v;
            return (
              <label key={String(v)} className={`flex cursor-pointer items-center gap-2 rounded-full py-2 pr-[18px] pl-3.5 text-sm font-medium text-portal-titulo has-focus-visible:outline-2 has-focus-visible:outline-portal-activo ${elegido ? "border-[1.5px] border-portal-activo bg-portal-activo-fondo" : "border border-portal-borde-campo bg-blanco"}`}>
                <input type="radio" name="relacion" className="sr-only" checked={elegido} onChange={() => setRelacion(v)} />
                <span aria-hidden className={`flex size-4 items-center justify-center rounded-full border-[1.5px] ${elegido ? "border-portal-activo" : "border-portal-borde-campo"}`}>
                  {elegido && <span className="size-2 rounded-full bg-portal-activo" />}
                </span>
                {v ? "Sí" : "No"}
              </label>
            );
          })}
        </div>
      </fieldset>
      <div className="w-[322px]">
        <CampoPortal etiqueta="Sueldo mensual (opcional)" name="sueldo" type="number" min={0} step="0.01" placeholder="Ej. 25000" defaultValue={empleo?.sueldo ?? ""} />
      </div>
      {error && <Alerta tipo="error">{error}</Alerta>}
      <div className="flex justify-end gap-2.5">
        <BotonPortal type="button" variante="contorno" onClick={onCerrar}>Cancelar</BotonPortal>
        <BotonPortal type="submit" disabled={enviando}>Guardar empleo</BotonPortal>
      </div>
    </form>
  );
}
