import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { WeatherForecast } from '../types/weather-forecast';

@Component({
  selector: 'app-forecasts',
  templateUrl: './forecasts.component.html',
  styleUrls: ['./forecasts.component.css']
})

export class ForecastsComponent implements OnInit {
  public forecasts: WeatherForecast[] = [];

  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) { }

  ngOnInit(): void {
    this.http.get<WeatherForecast[]>(this.baseUrl + 'weatherforecast')
      .subscribe(result => {
          this.forecasts = result;
        }, error =>
          console.error(error)
      );
  }

}
