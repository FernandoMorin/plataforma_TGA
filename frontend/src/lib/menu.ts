import {
  Briefcase, ChartColumn, ClipboardList, FileCheck, GraduationCap, Inbox, Network, User, UserCog, Users,
  type LucideIcon,
} from "lucide-react";

// Menú de cada rol (CONTEXTO_REDISENO.md, sección 3, y página Esqueleto de Figma).
// Es también la lista de páginas a las que cada rol puede entrar; la primera es la de inicio.

export type Rol = "Coordinador" | "Administrador" | "Profesor" | "Alumno" | "Egresado";
export type Pagina = { titulo: string; ruta: string; descripcion?: string };
export type Item = { titulo: string; icono: LucideIcon } & ({ ruta: string; descripcion?: string } | { hijos: Pagina[] });
export type Seccion = { etiqueta?: string; items: Item[] };

const gestion: Item[] = [
  {
    titulo: "Alumnos", icono: Users, hijos: [
      { titulo: "Consulta de alumnos", ruta: "/alumnos", descripcion: "Alumnos inscritos en la maestría." },
      { titulo: "Importar alumnos", ruta: "/alumnos/importar" },
    ],
  },
  { titulo: "Titulación", icono: GraduationCap, ruta: "/titulacion" },
  {
    titulo: "Comités tutoriales", icono: Network, hijos: [
      { titulo: "Consulta de comités", ruta: "/comites" },
      { titulo: "Participación docente", ruta: "/comites/participacion" },
    ],
  },
  {
    titulo: "Egresados", icono: User, hijos: [
      { titulo: "Consulta de egresados", ruta: "/egresados" },
      { titulo: "Alta de egresado", ruta: "/egresados/alta" },
    ],
  },
  {
    titulo: "Reportes", icono: ChartColumn, hijos: [
      { titulo: "Generar reporte", ruta: "/reportes" },
      { titulo: "Historial de reportes", ruta: "/reportes/historial" },
    ],
  },
  {
    titulo: "Usuarios", icono: UserCog, hijos: [
      { titulo: "Consulta de usuarios", ruta: "/usuarios", descripcion: "Cuentas de acceso al sistema." },
      { titulo: "Alta de usuario", ruta: "/usuarios/alta" },
    ],
  },
];

const profesor: Item[] = [
  { titulo: "Mis alumnos", icono: Users, ruta: "/profesor/alumnos" },
  { titulo: "Solicitudes de dirección", icono: Inbox, ruta: "/profesor/solicitudes" },
  { titulo: "Evaluaciones", icono: ClipboardList, ruta: "/profesor/evaluaciones", descripcion: "Evaluaciones de tus alumnos." },
];

export const menus: Record<Rol, Seccion[]> = {
  Coordinador: [{ items: gestion }, { etiqueta: "Como profesor", items: profesor }],
  Administrador: [{ items: gestion }],
  Profesor: [{ items: profesor }],
  Alumno: [
    {
      items: [
        {
          titulo: "Mi titulación", icono: GraduationCap, hijos: [
            { titulo: "Mi trabajo", ruta: "/mi-titulacion", descripcion: "Tu tesis y su avance." },
            { titulo: "Mi comité", ruta: "/mi-titulacion/comite" },
            { titulo: "Mis evaluaciones", ruta: "/mi-titulacion/evaluaciones" },
            { titulo: "Evidencias", ruta: "/mi-titulacion/evidencias" },
          ],
        },
        { titulo: "Requisito TOEFL", icono: FileCheck, ruta: "/toefl" },
      ],
    },
  ],
  Egresado: [
    {
      items: [
        { titulo: "Mi perfil", icono: User, ruta: "/mi-perfil", descripcion: "Tus datos personales y de contacto." },
        { titulo: "Historial laboral", icono: Briefcase, ruta: "/historial-laboral" },
        { titulo: "Constancias", icono: FileCheck, ruta: "/constancias" },
        { titulo: "Comentarios", icono: Inbox, ruta: "/comentarios" },
      ],
    },
  ],
};

export const miCuenta: Pagina = { titulo: "Mi cuenta", ruta: "/mi-cuenta" };

export const paginas = (rol: Rol): Pagina[] => [
  ...menus[rol].flatMap((s) => s.items.flatMap((i) => ("hijos" in i ? i.hijos : [i]))),
  miCuenta,
];

export const inicio = (rol: Rol) => paginas(rol)[0].ruta;

// Entrada del menú a la que pertenece una ruta: la más específica que la contiene
// (la ficha /alumnos/300101 pertenece a «Consulta de alumnos»). Sin entrada, el rol no tiene acceso.
export const paginaDe = (rol: Rol, ruta: string) =>
  paginas(rol)
    .filter((p) => ruta === p.ruta || ruta.startsWith(`${p.ruta}/`))
    .sort((a, b) => b.ruta.length - a.ruta.length)[0];

// Todas las páginas de todos los roles, sin repetir: las genera la ruta comodín del portal.
export const todasLasPaginas = (): Pagina[] => {
  const unicas = new Map<string, Pagina>();
  for (const rol of Object.keys(menus) as Rol[]) for (const p of paginas(rol)) unicas.set(p.ruta, p);
  return [...unicas.values()];
};

export const pagina = (ruta: string) => todasLasPaginas().find((p) => p.ruta === ruta);
