"use client";

import { useEffect, useState } from "react";
import { EncabezadoPagina } from "@/components/portal";
import { TarjetaInforme, type Informe } from "@/components/informe";
import { sesionVencida } from "@/lib/acceso";

// Mis evaluaciones (Figma AT5): un informe en proceso solo se anuncia; los cerrados muestran el acta.

export function MisEvaluaciones() {
  const [informes, setInformes] = useState<Informe[] | null>(null);

  useEffect(() => {
    fetch("/api/mi-titulacion/evaluaciones").then(async (res) => {
      if (res.status === 401) sesionVencida();
      else if (res.ok) setInformes(await res.json());
    });
  }, []);

  return (
    <>
      <EncabezadoPagina titulo="Mis evaluaciones" />
      {informes?.length === 0 && <p className="text-[15px] text-portal-subtexto">Aún no tienes evaluaciones.</p>}
      {informes?.map((i) => <TarjetaInforme key={i.id} informe={i} />)}
    </>
  );
}
