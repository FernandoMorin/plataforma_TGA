import Image from "next/image";

// Encabezado, barra de frase y pie del estándar institucional (Figma, página Login: 7:383, 7:396, 7:398).

export function Encabezado({ titulo }: { titulo: string }) {
  return (
    <header>
      <div className="bg-primario flex h-[112px] items-center justify-between gap-6 px-[116px] text-blanco">
        <div className="flex items-center gap-4">
          <Image src="/img/logo-uaslp.png" alt="UASLP" width={160} height={63} className="h-[63px] w-40 object-contain" priority />
          <div className="h-[68px] w-0.5 bg-blanco" aria-hidden />
          <p className="text-[21px]">{titulo}</p>
        </div>
        <p className="text-lg font-semibold">Aspirantes | Estudiantes | Egresados | Docentes | Administrativos</p>
      </div>
      <div className="h-2.5 bg-acento" />
    </header>
  );
}

export function BarraDeFrase() {
  return (
    <div className="bg-blanco flex justify-end px-[102px] py-3">
      <p className="font-bold">&quot;Siempre autónoma. Por mi patria educaré.&quot;</p>
    </div>
  );
}

const redes = ["facebook", "twitter", "youtube", "instagram"];
const direccion = [
  "UASLP",
  "Universidad Autónoma de San Luis Potosí",
  "Álvaro Obregón 64, Centro. CP 78000",
  "San Luis Potosí, SLP",
  "444 826 23 00",
  "© 2026 Todos los derechos reservados",
];
const enlaces = ["Universidad", "Oferta Educativa", "Investigación", "Cultura", "Vinculación", "Internacional", "Correo"];

export function PieDePagina() {
  return (
    <footer>
      <div className="h-2.5 bg-acento" />
      <div className="bg-primario flex justify-between px-[103px] py-[30px] text-lg text-blanco">
        <div className="flex flex-col gap-1">
          <div className="flex gap-3 pb-2.5">
            {redes.map((r) => (
              <Image key={r} src={`/img/${r}.svg`} alt={r} width={22} height={22} />
            ))}
          </div>
          {direccion.map((linea) => (
            <p key={linea}>{linea}</p>
          ))}
        </div>
        <ul className="flex flex-col gap-2 font-semibold">
          {enlaces.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      </div>
    </footer>
  );
}
