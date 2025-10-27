import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './footer.html',
  styleUrl: './footer.css'
})
export class Footer {
  newsletterEmail: string = '';
  currentYear: number = new Date().getFullYear();

  // Links de navegação organizados por seção
  aboutLinks = [
    { label: 'Sobre Nós', route: '/about' },
    { label: 'Nossa História', route: '/history' },
    { label: 'Carreiras', route: '/careers' },
    { label: 'Imprensa', route: '/press' }
  ];

  customerServiceLinks = [
    { label: 'Central de Ajuda', route: '/help' },
    { label: 'Fale Conosco', route: '/contact' },
    { label: 'Trocas e Devoluções', route: '/returns' },
    { label: 'Política de Privacidade', route: '/privacy' }
  ];

  categoryLinks = [
    { label: 'Eletrônicos', route: '/category/electronics' },
    { label: 'Roupas', route: '/category/clothing' },
    { label: 'Casa e Jardim', route: '/category/home-garden' },
    { label: 'Esportes', route: '/category/sports' }
  ];

  socialLinks = [
    { name: 'Facebook', url: 'https://facebook.com', icon: 'facebook' },
    { name: 'Instagram', url: 'https://instagram.com', icon: 'instagram' },
    { name: 'Twitter', url: 'https://twitter.com', icon: 'twitter' },
    { name: 'YouTube', url: 'https://youtube.com', icon: 'youtube' }
  ];

  paymentMethods = [
    { name: 'Visa', icon: 'visa' },
    { name: 'Mastercard', icon: 'mastercard' },
    { name: 'PIX', icon: 'pix' },
    { name: 'Boleto', icon: 'boleto' }
  ];

  onNewsletterSubmit() {
    if (this.newsletterEmail && this.isValidEmail(this.newsletterEmail)) {
      console.log('Newsletter subscription:', this.newsletterEmail);
      // Aqui você pode implementar a lógica de inscrição na newsletter
      this.newsletterEmail = '';
      alert('Obrigado por se inscrever em nossa newsletter!');
    } else {
      alert('Por favor, insira um email válido.');
    }
  }

  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
}
