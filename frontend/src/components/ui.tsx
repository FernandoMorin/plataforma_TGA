import type { ComponentProps, ReactNode } from "react";

// Componentes base de la página Login de Figma: Cabecera de tarjeta 7:425, Campo 7:439, Botón 7:446, Aviso 7:453.

export function Tarjeta({ titulo, children, className = "" }: { titulo: string; children: ReactNode; className?: string }) {
  return (
    <section className={`bg-blanco border border-borde rounded overflow-hidden ${className}`}>
      <h2 className="bg-tarjeta-cabecera border-b border-borde px-5 py-3.5 text-[23px] font-bold text-titulo">{titulo}</h2>
      {children}
    </section>
  );
}

// Estados de Figma: Vacío (blanco), Foco (borde azul con halo) y Lleno (relleno azul claro).
export function Campo({ etiqueta, id, ...input }: { etiqueta: string; id: string } & ComponentProps<"input">) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id}>{etiqueta}</label>
      <input
        id={id}
        className="bg-blanco border border-campo-borde rounded-md px-3 py-[9px] outline-none placeholder:text-placeholder focus:border-campo-foco focus:shadow-[0_0_0_4px_rgba(13,110,253,0.25)] not-placeholder-shown:not-focus:bg-campo-relleno not-placeholder-shown:not-focus:border-campo-relleno"
        {...input}
      />
    </div>
  );
}

export function Boton({ children, ...boton }: ComponentProps<"button">) {
  return (
    <button
      className="bg-boton text-blanco text-lg font-semibold rounded-full px-[26px] py-[9px] enabled:hover:bg-boton-hover disabled:opacity-50"
      {...boton}
    >
      {children}
    </button>
  );
}

const avisos = {
  error: "bg-error-fondo border-error-borde text-error-texto",
  info: "bg-info-fondo border-info-borde text-info-texto",
  exito: "bg-exito-fondo border-exito-borde text-exito-texto",
};

export function Aviso({ tipo = "error", children }: { tipo?: keyof typeof avisos; children: ReactNode }) {
  return (
    <p role={tipo === "error" ? "alert" : "status"} className={`border text-[15px] rounded-md px-4 py-3 ${avisos[tipo]}`}>
      {children}
    </p>
  );
}
