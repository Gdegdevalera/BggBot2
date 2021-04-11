import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AuthorizeService } from '../../api-authorization/authorize.service';
import { FeedItem, Subscription } from '../app.models';
import { ApiService } from '../services/api.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {

  private readonly reg = /^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#%[\]@!\$&'\(\)\*\+,;=.]+$/;

  public subscriptions: Subscription[];
  public userLoggedIn: boolean;
  public showForm: boolean;
  public form = this.formBuilder.group({
    feedUrl: ['', [Validators.required, Validators.pattern(this.reg)]]
  });
  public testItemsLoading: boolean;
  public testItems: FeedItem[];

  constructor(
    private api: ApiService,
    private formBuilder: FormBuilder,
    private authorize: AuthorizeService
  ) {
    api.getSubscriptions().subscribe(result => {
          this.subscriptions = result;
    });

    authorize.isAuthenticated().subscribe(result => {
      this.userLoggedIn = result;
    });
  }

  get feedUrl() {
    return this.form.get('feedUrl');
  }

  public toggleForm() {
    this.showForm = !this.showForm;
  }

  public stop(subscription: Subscription) {
    this.api.stopSubscription(subscription.id)
      .subscribe(result => {
        const ind = this.subscriptions.indexOf(subscription);
        this.subscriptions[ind] = result;
      });
  }

  public start(subscription: Subscription) {
    this.api.startSubscription(subscription.id)
      .subscribe(result => {
        const ind = this.subscriptions.indexOf(subscription);
        this.subscriptions[ind] = result;
      });
  }

  public onSubmit() {
    this.api.createSubscription(this.form.value)
        .subscribe(result => {
          this.subscriptions.push(result);
          this.showForm = false;
        });
  }

  public onTest() {
    this.testItems = null;
    this.testItemsLoading = true;
    this.api.testSubscription(this.form.value)
      .subscribe(result => {
        this.testItems = result;
      }, () => this.testItemsLoading = false);
  }
}
