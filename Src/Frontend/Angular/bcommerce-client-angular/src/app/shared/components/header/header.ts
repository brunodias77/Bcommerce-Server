import { Component, HostListener, signal } from '@angular/core';

@Component({
  selector: 'app-header',
  imports: [],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header {
  // Signal para controlar o estado do scroll
  isScrolled = signal(false);

  // Dados de exemplo
  contentItems = [
    {
      id: 1,
      title: 'Design Moderno',
      description:
        'Interface limpa e moderna que se adapta perfeitamente ao comportamento do usuário.',
    },
    {
      id: 2,
      title: 'Performance',
      description: 'Otimizado com Angular Signals para máxima performance e reatividade.',
    },
    {
      id: 3,
      title: 'Responsivo',
      description: 'Funciona perfeitamente em todos os dispositivos e tamanhos de tela.',
    },
    {
      id: 4,
      title: 'Tailwind CSS',
      description: 'Estilizado com Tailwind CSS v4 para um desenvolvimento rápido e eficiente.',
    },
    {
      id: 5,
      title: 'Animações Suaves',
      description: 'Transições elegantes que proporcionam uma experiência de usuário superior.',
    },
    {
      id: 6,
      title: 'Acessibilidade',
      description: 'Desenvolvido seguindo as melhores práticas de acessibilidade web.',
    },
  ];

  scrollContent = [
    'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.',
    'Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.',
    'Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo.',
    'Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet.',
    'At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident.',
  ];

  @HostListener('window:scroll', [])
  onWindowScroll() {
    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
    this.isScrolled.set(scrollTop > 50);
  }

  // Classes computadas - OTIMIZADAS
  headerClasses() {
    const base = 'fixed z-50 transition-all duration-300 ease-out bg-white shadow-md';
    return this.isScrolled()
      ? `${base} left-4 right-4 top-4 rounded-2xl`
      : `${base} left-0 right-0 top-0 rounded-none`;
  }

  containerClasses() {
    const base = 'flex items-center justify-between mx-auto transition-all duration-300';
    return this.isScrolled()
      ? `${base} max-w-6xl px-6 py-3`
      : `${base} max-w-7xl px-4 sm:px-6 lg:px-8 py-4`;
  }

  mainClasses() {
    return this.isScrolled() ? 'pt-20' : 'pt-24';
  }

  logoClasses() {
    return this.isScrolled()
      ? 'w-8 h-8 transition-all duration-300'
      : 'w-10 h-10 transition-all duration-300';
  }

  titleClasses() {
    const base = 'font-bold text-gray-900 transition-all duration-300';
    return this.isScrolled() ? `${base} text-xl` : `${base} text-2xl`;
  }

  navLinkClasses() {
    const base = 'text-gray-600 hover:text-blue-600 transition-colors font-medium';
    return this.isScrolled() ? `${base} text-sm` : `${base} text-base`;
  }

  ctaButtonClasses() {
    const base = 'bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-all';
    return this.isScrolled()
      ? `${base} px-4 py-2 text-sm`
      : `${base} px-6 py-3 font-semibold text-base`;
  }
}
